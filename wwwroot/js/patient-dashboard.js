(function () {
    const config = window.patientDashboardConfig || {};
    const mapsEnabled = !!config.mapsEnabled;
    const appointmentsWithLocation = Array.isArray(config.appointments) ? config.appointments : [];

    let singlePinMap;
    let overviewMap;
    let mapDisplayMode = "cards";

    const overviewEntries = [];
    const mapGroupedPoints = buildGroupedMapPoints(appointmentsWithLocation);

    function escapeHtml(value) {
        return String(value || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");
    }

    function buildGroupedMapPoints(source) {
        const byKey = new Map();
        source.forEach((item) => {
            const lat = Number(item.lat);
            const lng = Number(item.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
                return;
            }

            const key = `${item.doctorId || item.doctor}|${lat.toFixed(6)}|${lng.toFixed(6)}`;
            if (!byKey.has(key)) {
                byKey.set(key, {
                    doctor: item.doctor || "Doktor",
                    doctorPhoto: item.doctorPhoto || "/images/default-avatar.png",
                    doctorId: item.doctorId || "",
                    lat,
                    lng,
                    nearestTime: item.time || "-",
                    nearestTimeValue: item.timeValue || "",
                    nearestRelative: item.relative || "",
                    minPriceLabel: item.price || "-",
                    availableCount: 0
                });
            }

            const target = byKey.get(key);
            target.availableCount += 1;
            if (!target.nearestTimeValue || (item.timeValue && new Date(item.timeValue) < new Date(target.nearestTimeValue))) {
                target.nearestTime = item.time;
                target.nearestTimeValue = item.timeValue || target.nearestTimeValue;
                target.nearestRelative = item.relative || target.nearestRelative;
            }
        });

        return Array.from(byKey.values());
    }

    function whenMapsReady(callback) {
        if (!mapsEnabled) {
            return;
        }

        if (window.google && window.google.maps) {
            callback();
            return;
        }

        const startedAt = Date.now();
        const timer = window.setInterval(function () {
            if (window.google && window.google.maps) {
                window.clearInterval(timer);
                callback();
                return;
            }

            if (Date.now() - startedAt > 15000) {
                window.clearInterval(timer);
            }
        }, 100);
    }

    function asValidPosition(lat, lng) {
        if (Number.isFinite(lat) && Number.isFinite(lng)) {
            return { lat, lng };
        }
        return null;
    }

    function getGlobalLocation() {
        const gl = window.mentoraGlobalLocation || {};
        const lat = Number(gl.latitude ?? config.globalLat);
        const lng = Number(gl.longitude ?? config.globalLng);
        return asValidPosition(lat, lng);
    }

    function updateActiveLocationBadge() {
        const el = document.getElementById("activeLocationBadge");
        if (!el) {
            return;
        }

        const label = (window.mentoraGlobalLocation && window.mentoraGlobalLocation.label)
            ? window.mentoraGlobalLocation.label
            : (config.globalLocationLabel || "Konum secilmedi");

        el.textContent = `Konum: ${label}`;
    }

    function updateRouteLinks() {
        const origin = getGlobalLocation();
        const hasOrigin = !!origin;

        document.querySelectorAll("a.route-link[data-destination]").forEach((link) => {
            const destination = link.getAttribute("data-destination");
            if (!destination) {
                return;
            }

            let href = `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}&travelmode=driving`;
            if (hasOrigin) {
                href += `&origin=${origin.lat},${origin.lng}`;
            }
            link.setAttribute("href", href);
        });
    }

    function buildInfoCard(point, entryId) {
        const availableText = point.availableCount === 1
            ? "1 boş seans"
            : `${point.availableCount} boş seans`;

        const safeDoctorName = escapeHtml(point.doctor);
        const safeRelative = escapeHtml(point.nearestRelative || "");
        const safeTime = escapeHtml(point.nearestTime || "-");
        const safePrice = escapeHtml(point.minPriceLabel || "-");
        const safePhoto = escapeHtml(point.doctorPhoto || "/images/default-avatar.png");

        return `
            <div class="map-info-card" data-map-entry="${entryId}">
                <div class="map-info-slot-count">${availableText}</div>
                <button type="button" class="map-info-close" data-map-close="${entryId}" aria-label="Kapat">X</button>
                <img src="${safePhoto}" alt="${safeDoctorName}" class="map-info-avatar" />
                <div class="map-info-text">
                    <div class="map-info-name">${safeDoctorName}</div>
                    <div class="map-info-meta">${safeRelative}</div>
                    <div class="map-info-meta">Tarih: ${safeTime}</div>
                    <div class="map-info-meta">Fiyat: ${safePrice}</div>
                </div>
            </div>`;
    }

    function buildNameTooltip(point) {
        return `<div class="map-hover-name">${escapeHtml(point.doctor)}</div>`;
    }

    function clearOverviewEntries() {
        overviewEntries.forEach((entry) => {
            if (entry.hoverTimer) {
                window.clearTimeout(entry.hoverTimer);
            }
            entry.cardWindow?.close();
            entry.nameWindow?.close();
            if (entry.marker) {
                entry.marker.setMap(null);
            }
        });
        overviewEntries.length = 0;
    }

    function closeAllCardWindows() {
        overviewEntries.forEach((entry) => entry.cardWindow?.close());
    }

    function closeEntryCard(entry) {
        if (!entry) {
            return;
        }
        entry.cardWindow?.close();
        entry.state = "pin";
        if (mapDisplayMode === "cards") {
            entry.marker?.setMap(overviewMap);
        }
    }

    function openEntryCard(entry) {
        if (!entry || !overviewMap) {
            return;
        }
        entry.state = "card";
        entry.cardWindow?.setPosition(entry.position);
        entry.cardWindow?.open({ map: overviewMap, anchor: entry.marker || undefined });
    }

    function closeAllCardsExcept(activeEntry) {
        overviewEntries.forEach((entry) => {
            if (entry !== activeEntry) {
                entry.cardWindow?.close();
                entry.state = "pin";
            }
        });
    }

    function getPinLabel(point) {
        const parts = String(point.doctor || "").trim().split(/\s+/).filter(Boolean);
        if (parts.length === 0) {
            return "Doktor";
        }
        if (parts.length === 1) {
            return parts[0];
        }
        return `${parts[0]} ${parts[1]}`;
    }

    function applyMapDisplayMode() {
        if (!overviewMap) {
            return;
        }

        overviewEntries.forEach((entry) => {
            entry.nameWindow?.close();
            if (entry.hoverTimer) {
                window.clearTimeout(entry.hoverTimer);
                entry.hoverTimer = null;
            }

            if (mapDisplayMode === "cards") {
                entry.marker?.setMap(null);
                entry.cardWindow?.setPosition(entry.position);
                entry.cardWindow?.open({ map: overviewMap });
            } else {
                entry.cardWindow?.close();
            }

            if (entry.marker) {
                entry.marker.setMap(overviewMap);
                entry.marker.setLabel(mapDisplayMode === "pin-name"
                    ? {
                        text: getPinLabel(entry.point),
                        color: "#1f2937",
                        fontWeight: "600",
                        fontSize: "12px"
                    }
                    : null);
            }
        });

        document.querySelectorAll(".map-mode-btn").forEach((button) => {
            const isActive = button.getAttribute("data-map-mode") === mapDisplayMode;
            button.classList.toggle("active", isActive);
            button.classList.toggle("btn-primary", isActive);
            button.classList.toggle("btn-outline-secondary", !isActive);
        });
    }

    function bindInfoWindowClose(entry) {
        google.maps.event.addListener(entry.cardWindow, "domready", function () {
            document.querySelectorAll(`[data-map-close="${entry.id}"]`).forEach((btn) => {
                btn.addEventListener("click", function () {
                    closeEntryCard(entry);
                });
            });
        });

        google.maps.event.addListener(entry.cardWindow, "closeclick", function () {
            closeEntryCard(entry);
        });
    }

    function initSinglePinMap(lat, lon, doctorName, doctorPhoto, time, price) {
        const location = asValidPosition(lat, lon);
        if (!location) {
            return;
        }

        const mapElement = document.getElementById("map-modal-body");
        if (!mapElement) {
            return;
        }

        singlePinMap = new google.maps.Map(mapElement, {
            zoom: 14,
            center: location
        });

        const marker = new google.maps.Marker({
            position: location,
            map: singlePinMap,
            title: doctorName || "Doktor"
        });

        const point = {
            doctor: doctorName || "Doktor",
            doctorPhoto: doctorPhoto || "/images/default-avatar.png",
            nearestTime: time || "-",
            nearestRelative: "",
            minPriceLabel: price || "-",
            availableCount: 1
        };

        const infoWindow = new google.maps.InfoWindow({
            content: buildInfoCard(point, "single")
        });
        infoWindow.addListener("domready", function () {
            document.querySelectorAll('[data-map-close="single"]').forEach((btn) => {
                btn.addEventListener("click", function () {
                    infoWindow.close();
                });
            });
        });
        infoWindow.open({ map: singlePinMap, anchor: marker });
    }

    function initOverviewMap() {
        const mapElement = document.getElementById("appointments-map");
        if (!mapElement) {
            return;
        }

        const globalOrigin = getGlobalLocation();
        const fallbackCenter = globalOrigin ||
            (mapGroupedPoints.length > 0
                ? { lat: Number(mapGroupedPoints[0].lat), lng: Number(mapGroupedPoints[0].lng) }
                : { lat: 39.925533, lng: 32.866287 });

        overviewMap = new google.maps.Map(mapElement, {
            center: fallbackCenter,
            zoom: 6
        });

        clearOverviewEntries();
        const bounds = new google.maps.LatLngBounds();
        let hasAnyMarker = false;

        if (globalOrigin) {
            const userMarker = new google.maps.Marker({
                position: globalOrigin,
                map: overviewMap,
                title: "Seçili global konum",
                icon: {
                    path: google.maps.SymbolPath.CIRCLE,
                    scale: 8,
                    fillColor: "#2563eb",
                    fillOpacity: 1,
                    strokeWeight: 2,
                    strokeColor: "#ffffff"
                }
            });
            bounds.extend(userMarker.getPosition());
            hasAnyMarker = true;
        }

        mapGroupedPoints.forEach((point, index) => {
            const position = asValidPosition(Number(point.lat), Number(point.lng));
            if (!position) {
                return;
            }

            const marker = new google.maps.Marker({
                position,
                map: null,
                title: point.doctor
            });

            const entryId = `overview-${index}`;
            const cardWindow = new google.maps.InfoWindow({ content: buildInfoCard(point, entryId), disableAutoPan: false });
            const nameWindow = new google.maps.InfoWindow({ content: buildNameTooltip(point), disableAutoPan: true });
            const entry = {
                id: entryId,
                point,
                position,
                marker,
                cardWindow,
                nameWindow,
                state: "card",
                hoverTimer: null
            };
            bindInfoWindowClose(entry);

            marker.addListener("click", function () {
                if (mapDisplayMode !== "cards") {
                    closeAllCardsExcept(entry);
                }
                openEntryCard(entry);
            });

            marker.addListener("mouseover", function () {
                if (mapDisplayMode !== "pin" || entry.state !== "pin") {
                    return;
                }
                entry.hoverTimer = window.setTimeout(function () {
                    entry.nameWindow.open({ map: overviewMap, anchor: marker });
                }, 700);
            });

            marker.addListener("mouseout", function () {
                if (entry.hoverTimer) {
                    window.clearTimeout(entry.hoverTimer);
                    entry.hoverTimer = null;
                }
                entry.nameWindow.close();
            });

            overviewEntries.push(entry);
            bounds.extend(marker.getPosition());
            hasAnyMarker = true;
        });

        applyMapDisplayMode();
        if (hasAnyMarker) {
            overviewMap.fitBounds(bounds);
        }
    }

    function init() {
        const filterForm = document.getElementById("filterForm");
        const sortSelector = document.getElementById("sortSelector");
        const sortByInput = document.getElementById("sortBy");
        const sortDirectionInput = document.getElementById("sortDirection");
        const sortDirectionToggle = document.getElementById("sortDirectionToggle");
        const latInput = document.getElementById("userLat");
        const lonInput = document.getElementById("userLon");

        updateActiveLocationBadge();
        updateRouteLinks();

        const globalOrigin = getGlobalLocation();
        if (globalOrigin && latInput && lonInput) {
            latInput.value = globalOrigin.lat.toFixed(6);
            lonInput.value = globalOrigin.lng.toFixed(6);
        }

        document.addEventListener("mentora:location-changed", function (event) {
            const detail = event.detail || {};
            if (latInput && lonInput && Number.isFinite(detail.latitude) && Number.isFinite(detail.longitude)) {
                latInput.value = Number(detail.latitude).toFixed(6);
                lonInput.value = Number(detail.longitude).toFixed(6);
            }
            updateActiveLocationBadge();
            updateRouteLinks();
            submitFilterKeepingScroll();
        });

        function submitFilterKeepingScroll() {
            if (!filterForm) {
                return;
            }
            sessionStorage.setItem("mentora:patientDashboard:scrollY", String(window.scrollY || 0));
            filterForm.requestSubmit();
        }

        function restoreScroll() {
            const value = sessionStorage.getItem("mentora:patientDashboard:scrollY");
            if (!value) {
                return;
            }
            sessionStorage.removeItem("mentora:patientDashboard:scrollY");
            const y = Number(value);
            if (Number.isFinite(y) && y > 0) {
                window.scrollTo({ top: y, behavior: "auto" });
            }
        }

        restoreScroll();

        document.getElementById("locationModal")?.addEventListener("show.bs.modal", function (event) {
            if (!mapsEnabled) {
                return;
            }

            const button = event.relatedTarget;
            if (!button) {
                return;
            }

            const lat = parseFloat(button.getAttribute("data-lat"));
            const lon = parseFloat(button.getAttribute("data-lon"));
            const doctor = button.getAttribute("data-doctor") || "Doktor";
            const doctorPhoto = button.getAttribute("data-doctor-photo") || "/images/default-avatar.png";
            const price = button.getAttribute("data-price") || "-";
            const time = button.getAttribute("data-time") || "-";

            const modalTitle = this.querySelector(".modal-title");
            if (modalTitle) {
                modalTitle.textContent = `${doctor} - Konum`;
            }

            whenMapsReady(function () {
                window.setTimeout(() => initSinglePinMap(lat, lon, doctor, doctorPhoto, time, price), 120);
            });
        });

        document.getElementById("appointmentsMapModal")?.addEventListener("shown.bs.modal", function () {
            if (!mapsEnabled) {
                return;
            }
            whenMapsReady(function () {
                initOverviewMap();
                if (overviewMap) {
                    window.setTimeout(() => google.maps.event.trigger(overviewMap, "resize"), 120);
                }
            });
        });

        document.querySelectorAll(".map-mode-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                const mode = btn.getAttribute("data-map-mode");
                if (!mode) {
                    return;
                }

                if (mode === "cards") {
                    overviewEntries.forEach((entry) => {
                        entry.state = "card";
                    });
                } else {
                    overviewEntries.forEach((entry) => {
                        entry.state = "pin";
                        entry.cardWindow?.close();
                    });
                }
                mapDisplayMode = mode;
                applyMapDisplayMode();
            });
        });

        document.getElementById("requestModal")?.addEventListener("show.bs.modal", function (event) {
            const button = event.relatedTarget;
            if (!button) {
                return;
            }

            const appointmentIdEl = document.getElementById("modalAppointmentId");
            const doctorIdEl = document.getElementById("modalDoctorId");
            const doctorText = document.getElementById("requestModalDoctor");
            const timeText = document.getElementById("requestModalTime");
            const typeText = document.getElementById("requestModalType");
            const priceText = document.getElementById("requestModalPrice");
            if (appointmentIdEl) appointmentIdEl.value = button.getAttribute("data-appointment-id");
            if (doctorIdEl) doctorIdEl.value = button.getAttribute("data-doctor-id");
            if (doctorText) doctorText.textContent = button.getAttribute("data-doctor-name") || "-";
            if (timeText) timeText.textContent = button.getAttribute("data-appointment-time") || "-";
            if (typeText) typeText.textContent = button.getAttribute("data-appointment-type") || "-";
            if (priceText) priceText.textContent = button.getAttribute("data-appointment-price") || "-";

            this.querySelectorAll("textarea, input:not([type='hidden']), select").forEach((field) => {
                field.value = "";
            });
        });

        function syncSortAndSubmit() {
            if (sortByInput && sortSelector) {
                sortByInput.value = sortSelector.value;
            }
            if (sortDirectionInput && sortDirectionToggle) {
                sortDirectionToggle.textContent = sortDirectionInput.value === "asc" ? "Artan" : "Azalan";
            }
            submitFilterKeepingScroll();
        }

        sortSelector?.addEventListener("change", syncSortAndSubmit);
        sortDirectionToggle?.addEventListener("click", function () {
            if (!sortDirectionInput) {
                return;
            }
            sortDirectionInput.value = sortDirectionInput.value === "asc" ? "desc" : "asc";
            syncSortAndSubmit();
        });

        if (sortSelector && sortByInput && !sortByInput.value) {
            sortByInput.value = sortSelector.value;
        }

        const autoSubmitSelectors = [
            "input[name='Filter.StartDate']",
            "input[name='Filter.EndDate']",
            "input[name='Filter.MinPrice']",
            "input[name='Filter.MaxPrice']",
            "input[name='Filter.DistanceKm']",
            "input[name='Filter.IsOnline']",
            "input[name='Filter.IsInPerson']",
            "input[name='Filter.SelectedDays']",
            "input[name='Filter.SelectedDoctorIds']",
            "input[name='Filter.SelectedSpecialtyIds']"
        ];

        autoSubmitSelectors.forEach((selector) => {
            filterForm?.querySelectorAll(selector).forEach((input) => {
                input.addEventListener("change", submitFilterKeepingScroll);
            });
        });
    }

    document.addEventListener("DOMContentLoaded", init);
})();
