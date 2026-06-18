(function () {
    const config = window.mentoraNavbarConfig || {};
    const antiForgeryInput = document.querySelector("input[name='__RequestVerificationToken']");
    const antiForgeryToken = antiForgeryInput ? antiForgeryInput.value : "";

    let locationState = normalizeLocation(config.initialGlobalLocation);
    let mapInstance = null;
    let mapMarker = null;
    let mapAutocomplete = null;
    let mapDraftSelection = null;

    function normalizeLocation(raw) {
        if (!raw) {
            return {
                source: "Profile",
                latitude: null,
                longitude: null,
                label: "Konum secilmedi",
                hasCoordinates: false
            };
        }

        return {
            source: raw.source || "Profile",
            latitude: Number.isFinite(Number(raw.latitude)) ? Number(raw.latitude) : null,
            longitude: Number.isFinite(Number(raw.longitude)) ? Number(raw.longitude) : null,
            label: raw.label || "Konum secilmedi",
            hasCoordinates: !!raw.hasCoordinates || (Number.isFinite(Number(raw.latitude)) && Number.isFinite(Number(raw.longitude)))
        };
    }

    function setLocationState(newState, emit) {
        locationState = normalizeLocation(newState);
        window.mentoraGlobalLocation = locationState;
        updateLocationUi();
        updateRouteLinksWithGlobalOrigin();
        if (emit) {
            document.dispatchEvent(new CustomEvent("mentora:location-changed", { detail: locationState }));
        }
    }

    function updateLocationUi() {
        const badge = document.getElementById("globalLocationBadgeText");
        if (badge) {
            badge.textContent = locationState.label || "Konum secilmedi";
        }

        const sourceSelect = document.getElementById("globalLocationSourceSelect");
        if (sourceSelect) {
            sourceSelect.value = locationState.source || "Profile";
        }

        const meta = document.getElementById("globalLocationMeta");
        if (meta) {
            const sourceLabelMap = {
                "Profile": "Profil",
                "DeviceGps": "Cihaz",
                "ManualMap": "Manuel"
            };
            const sourceLabel = sourceLabelMap[locationState.source] || locationState.source;
            if (locationState.hasCoordinates) {
                meta.innerHTML = `<div><strong>Kaynak:</strong> ${sourceLabel}</div>` +
                    `<div><strong>Enlem:</strong> ${locationState.latitude.toFixed(6)}</div>` +
                    `<div><strong>Boylam:</strong> ${locationState.longitude.toFixed(6)}</div>`;
            } else {
                meta.innerHTML = `<div><strong>Kaynak:</strong> ${sourceLabel}</div>` +
                    `<div class="text-warning">Koordinat tanimli degil</div>`;
            }
        }
    }

    function updateRouteLinksWithGlobalOrigin() {
        const hasOrigin = Number.isFinite(locationState.latitude) && Number.isFinite(locationState.longitude);
        document.querySelectorAll("a.route-link[data-destination]").forEach((link) => {
            const destination = link.getAttribute("data-destination");
            if (!destination) {
                return;
            }

            let href = `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}&travelmode=driving`;
            if (hasOrigin) {
                href += `&origin=${locationState.latitude},${locationState.longitude}`;
            }
            link.setAttribute("href", href);
        });
    }

    function requestJson(url, method, body) {
        return fetch(url, {
            method: method || "GET",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": antiForgeryToken
            },
            body: body ? JSON.stringify(body) : undefined,
            credentials: "same-origin"
        }).then(async (res) => {
            if (!res.ok) {
                throw new Error(`HTTP ${res.status}`);
            }
            return res.status === 204 ? null : res.json();
        });
    }

    async function updateGlobalLocationOnServer(payload) {
        const updated = await requestJson("/Location/Context", "POST", payload);
        setLocationState(updated, true);
    }

    function resolveDeviceLocation() {
        return new Promise((resolve, reject) => {
            if (navigator.geolocation) {
                navigator.geolocation.getCurrentPosition(
                    (position) => resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    }),
                    () => {
                        fetch("https://ipapi.co/json/")
                            .then(r => r.json())
                            .then(data => {
                                if (data.latitude && data.longitude) {
                                    resolve({ latitude: data.latitude, longitude: data.longitude, source: "ip" });
                                } else {
                                    reject(new Error("Konum alinamadi. Lutfen haritadan manuel secin."));
                                }
                            })
                            .catch(() => reject(new Error("Konum alinamadi. Lutfen haritadan manuel secin.")));
                    },
                    { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
                );
            } else {
                reject(new Error("Tarayiciniz konum hizmetini desteklemiyor."));
            }
        });
    }

    function ensureMapsLoaded() {
        return new Promise((resolve, reject) => {
            if (window.google && window.google.maps) {
                resolve();
                return;
            }

            if (!config.hasMapsApiKey || !config.mapsApiKey) {
                reject(new Error("Google Maps anahtari tanimli degil."));
                return;
            }

            const existing = document.querySelector("script[data-mentora-global-map]");
            if (existing) {
                const timer = window.setInterval(function () {
                    if (window.google && window.google.maps) {
                        window.clearInterval(timer);
                        resolve();
                    }
                }, 80);
                return;
            }

            const script = document.createElement("script");
            script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(config.mapsApiKey)}&libraries=places&loading=async`;
            script.async = true;
            script.defer = true;
            script.setAttribute("data-mentora-global-map", "1");
            script.onload = () => resolve();
            script.onerror = () => reject(new Error("Google Maps yuklenemedi."));
            document.head.appendChild(script);
        });
    }

    function initLocationMapModal() {
        const mapCanvas = document.getElementById("globalLocationMapCanvas");
        if (!mapCanvas) {
            return;
        }

        const initial = {
            lat: Number.isFinite(locationState.latitude) ? locationState.latitude : 39.925533,
            lng: Number.isFinite(locationState.longitude) ? locationState.longitude : 32.866287
        };

        if (!mapInstance) {
            mapInstance = new google.maps.Map(mapCanvas, {
                center: initial,
                zoom: Number.isFinite(locationState.latitude) ? 13 : 6
            });

            mapMarker = new google.maps.Marker({
                map: mapInstance,
                draggable: true,
                position: initial
            });

            mapDraftSelection = { latitude: initial.lat, longitude: initial.lng };

            mapInstance.addListener("click", (event) => {
                const lat = event.latLng.lat();
                const lng = event.latLng.lng();
                mapMarker.setPosition(event.latLng);
                mapDraftSelection = { latitude: lat, longitude: lng };
            });

            mapMarker.addListener("dragend", (event) => {
                mapDraftSelection = { latitude: event.latLng.lat(), longitude: event.latLng.lng() };
            });

            const search = document.getElementById("globalLocationMapSearch");
            if (search) {
                mapAutocomplete = new google.maps.places.Autocomplete(search, { fields: ["geometry"] });
                mapAutocomplete.addListener("place_changed", function () {
                    const place = mapAutocomplete.getPlace();
                    if (!place || !place.geometry || !place.geometry.location) {
                        return;
                    }
                    mapInstance.panTo(place.geometry.location);
                    mapInstance.setZoom(14);
                    mapMarker.setPosition(place.geometry.location);
                    mapDraftSelection = {
                        latitude: place.geometry.location.lat(),
                        longitude: place.geometry.location.lng()
                    };
                });
            }
        } else {
            mapInstance.panTo(initial);
            mapMarker.setPosition(initial);
            mapDraftSelection = { latitude: initial.lat, longitude: initial.lng };
            window.setTimeout(() => google.maps.event.trigger(mapInstance, "resize"), 120);
        }
    }

    function bindLocationActions() {
        const sourceSelect = document.getElementById("globalLocationSourceSelect");
        const useDeviceBtn = document.getElementById("globalLocationUseDeviceBtn");
        const openMapBtn = document.getElementById("globalLocationOpenMapBtn");
        const saveProfileBtn = document.getElementById("globalLocationSaveProfileBtn");
        const applyMapBtn = document.getElementById("globalLocationApplyMapBtn");
        const mapModalEl = document.getElementById("globalLocationMapModal");

        sourceSelect?.addEventListener("change", async function () {
            const selected = sourceSelect.value || "Profile";
            if (selected === "Profile") {
                try {
                    const updated = await requestJson("/Location/Context", "POST", { source: "Profile" });
                    setLocationState(updated, true);
                } catch {
                    window.showMentoraToast?.("Profil konumu secilemedi.", "warning");
                }
            }
        });

        useDeviceBtn?.addEventListener("click", async function () {
            try {
                const gps = await resolveDeviceLocation();
                await updateGlobalLocationOnServer({
                    source: "DeviceGps",
                    latitude: gps.latitude,
                    longitude: gps.longitude,
                    label: "Cihaz"
                });
                window.showMentoraToast?.("Cihaz konumu güncellendi.", "success");
            } catch (error) {
                window.showMentoraToast?.(error.message || "Cihaz konumu alinamadi.", "warning");
            }
        });

        openMapBtn?.addEventListener("click", async function () {
            try {
                await ensureMapsLoaded();
                if (mapModalEl) {
                    bootstrap.Modal.getOrCreateInstance(mapModalEl).show();
                }
                initLocationMapModal();
            } catch (error) {
                window.showMentoraToast?.(error.message || "Harita acilamadi.", "warning");
            }
        });

        mapModalEl?.addEventListener("shown.bs.modal", async function () {
            try {
                await ensureMapsLoaded();
                initLocationMapModal();
            } catch {
                // no-op
            }
        });

        applyMapBtn?.addEventListener("click", async function () {
            if (!mapDraftSelection) {
                window.showMentoraToast?.("Haritadan konum secin.", "warning");
                return;
            }

            try {
                await updateGlobalLocationOnServer({
                    source: "ManualMap",
                    latitude: mapDraftSelection.latitude,
                    longitude: mapDraftSelection.longitude,
                    label: "Manuel"
                });
                window.showMentoraToast?.("Manuel konum uygulandi.", "success");
            } catch {
                window.showMentoraToast?.("Manuel konum kaydedilemedi.", "warning");
            }
        });

        saveProfileBtn?.addEventListener("click", async function () {
            try {
                const updated = await requestJson("/Location/Context/SaveProfile", "POST");
                setLocationState(updated, true);
                window.showMentoraToast?.("Konum profile kaydedildi.", "success");
            } catch {
                window.showMentoraToast?.("Konum profile kaydedilemedi.", "warning");
            }
        });
    }

    function formatDate(utcString) {
        const date = new Date(utcString);
        return Number.isNaN(date.getTime()) ? "" : date.toLocaleString("tr-TR");
    }

    function renderNotificationList(payload) {
        const container = document.getElementById("notificationListContainer");
        const unreadBadge = document.getElementById("navbarUnreadNotificationCount");
        if (!container) {
            return;
        }

        const unread = Number(payload.unreadCount || 0);
        if (unreadBadge) {
            unreadBadge.textContent = String(unread);
            unreadBadge.classList.toggle("d-none", unread === 0);
        }

        const items = Array.isArray(payload.items) ? payload.items : [];
        if (items.length === 0) {
            container.innerHTML = "<div class='px-3 py-2 small text-muted'>Bildirim yok.</div>";
            return;
        }

        container.innerHTML = items.map((item) => {
            const cls = item.isRead ? "notification-item is-read" : "notification-item";
            const deepLink = item.deepLink ? `data-deeplink='${item.deepLink}'` : "";
            return `
                <button type="button" class="${cls}" data-id="${item.id}" ${deepLink}>
                    <div class="notification-item-title">${item.title || ""}</div>
                    <div class="notification-item-message">${item.message || ""}</div>
                    <div class="notification-item-time">${formatDate(item.createdAtUtc)}</div>
                </button>`;
        }).join("");
    }

    function bindNotificationActions() {
        const dropdown = document.getElementById("notificationDropdown");
        const markAllBtn = document.getElementById("markAllNotificationsReadBtn");
        const container = document.getElementById("notificationListContainer");

        async function loadRecent() {
            if (!config.notificationRecentEndpoint) {
                return;
            }

            try {
                const payload = await requestJson(config.notificationRecentEndpoint, "GET");
                renderNotificationList(payload || {});
            } catch {
                if (container) {
                    container.innerHTML = "<div class='px-3 py-2 small text-danger'>Bildirimler yuklenemedi.</div>";
                }
            }
        }

        dropdown?.addEventListener("show.bs.dropdown", function () {
            loadRecent();
        });

        markAllBtn?.addEventListener("click", async function () {
            if (!config.notificationMarkAllReadEndpoint) {
                return;
            }
            await requestJson(config.notificationMarkAllReadEndpoint, "POST");
            await loadRecent();
        });

        container?.addEventListener("click", async function (event) {
            const target = event.target.closest(".notification-item");
            if (!target) {
                return;
            }

            const id = Number(target.getAttribute("data-id"));
            if (Number.isFinite(id) && config.notificationMarkReadEndpointBase) {
                const url = config.notificationMarkReadEndpointBase.replace("__id__", String(id));
                await requestJson(url, "POST");
            }

            const deepLink = target.getAttribute("data-deeplink");
            if (deepLink) {
                window.location.href = deepLink;
                return;
            }

            await loadRecent();
        });
    }

    function init() {
        setLocationState(locationState, false);
        bindLocationActions();
        bindNotificationActions();
    }

    document.addEventListener("DOMContentLoaded", init);
})();
