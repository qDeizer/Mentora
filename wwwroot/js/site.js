document.addEventListener("DOMContentLoaded", function () {
    initAppearancePreferences();
    initAppToasts();
    initConfirmModal();
    initRegisterTypeSwitcher();
    initProfilePhotoPreview();
    initCertificatePreview();
    initFloatingLabels();
    initLocationNoteHistory();
});

function initAppearancePreferences() {
    const themeSelect = document.getElementById("themePreferenceSelect");
    const densitySelect = document.getElementById("layoutDensitySelect");
    const form = document.getElementById("appearancePreferenceForm");

    const readTheme = () => localStorage.getItem("mentora:theme") || document.documentElement.dataset.themePreference || "system";
    const readDensity = () => localStorage.getItem("mentora:density") || document.documentElement.dataset.density || "comfortable";

    function resolveTheme(theme) {
        if (theme !== "system") {
            return theme;
        }

        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function applyAppearance(theme, density) {
        document.documentElement.dataset.themePreference = theme;
        document.documentElement.dataset.theme = resolveTheme(theme);
        document.documentElement.dataset.density = density;
        localStorage.setItem("mentora:theme", theme);
        localStorage.setItem("mentora:density", density);
    }

    async function saveAppearance(theme, density) {
        if (!document.body || document.body.dataset.signedIn !== "true" || !form) {
            return;
        }

        const token = form.querySelector("input[name='__RequestVerificationToken']")?.value || "";
        const body = new URLSearchParams();
        body.set("themePreference", theme);
        body.set("layoutDensity", density);
        body.set("__RequestVerificationToken", token);

        try {
            await fetch(document.body.dataset.appearanceSaveUrl || "/Settings/Appearance", {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
                },
                body
            });
        } catch {
            if (window.showMentoraToast) {
                window.showMentoraToast("Görünüm tercihi kaydedilemedi, bu tarayıcıda uygulanmaya devam edecek.", "warning");
            }
        }
    }

    const initialTheme = readTheme();
    const initialDensity = readDensity();
    applyAppearance(initialTheme, initialDensity);

    if (themeSelect) {
        themeSelect.value = initialTheme;
        themeSelect.addEventListener("change", function () {
            const theme = themeSelect.value || "system";
            const density = densitySelect?.value || readDensity();
            applyAppearance(theme, density);
            saveAppearance(theme, density);
        });
    }

    if (densitySelect) {
        densitySelect.value = initialDensity;
        densitySelect.addEventListener("change", function () {
            const theme = themeSelect?.value || readTheme();
            const density = densitySelect.value || "comfortable";
            applyAppearance(theme, density);
            saveAppearance(theme, density);
        });
    }

    if (window.matchMedia) {
        const media = window.matchMedia("(prefers-color-scheme: dark)");
        media.addEventListener?.("change", function () {
            if ((themeSelect?.value || readTheme()) === "system") {
                applyAppearance("system", densitySelect?.value || readDensity());
            }
        });
    }
}

function initAppToasts() {
    let container = document.getElementById("appToastContainer");
    if (!container) {
        container = document.createElement("div");
        container.id = "appToastContainer";
        container.className = "toast-container position-fixed top-0 end-0 p-3";
        document.body.appendChild(container);
    }

    window.showMentoraToast = function (message, type) {
        if (!container || !message) {
            return;
        }

        const level = (type || "warning").toLowerCase();
        const bgClass = level === "error" || level === "danger"
            ? "text-bg-danger"
            : level === "success"
                ? "text-bg-success"
                : "text-bg-warning";

        const toastEl = document.createElement("div");
        toastEl.className = `toast align-items-center border-0 ${bgClass}`;
        toastEl.role = "alert";
        toastEl.ariaLive = "assertive";
        toastEl.ariaAtomic = "true";
        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Kapat"></button>
            </div>`;
        container.appendChild(toastEl);

        const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
        toast.show();
        toastEl.addEventListener("hidden.bs.toast", function () {
            toastEl.remove();
        });
    };
}

function initConfirmModal() {
    if (!window.bootstrap || !document.body) {
        return;
    }

    const modalEl = document.createElement("div");
    modalEl.className = "modal fade";
    modalEl.id = "appConfirmModal";
    modalEl.tabIndex = -1;
    modalEl.setAttribute("aria-hidden", "true");
    modalEl.innerHTML = `
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content app-card">
                <div class="modal-header">
                    <h5 class="modal-title">Onay Gerekli</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Kapat"></button>
                </div>
                <div class="modal-body">
                    <p id="appConfirmMessage" class="mb-0">Bu islemi onayliyor musunuz?</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Vazgeç</button>
                    <button type="button" class="btn btn-danger" id="appConfirmApproveBtn">Onayla</button>
                </div>
            </div>
        </div>`;

    document.body.appendChild(modalEl);

    const modal = new bootstrap.Modal(modalEl);
    const messageEl = modalEl.querySelector("#appConfirmMessage");
    const approveBtn = modalEl.querySelector("#appConfirmApproveBtn");
    let pendingForm = null;

    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (!form.hasAttribute("data-confirm")) {
            return;
        }

        if (form.dataset.confirmed === "1") {
            form.dataset.confirmed = "0";
            return;
        }

        event.preventDefault();
        pendingForm = form;
        if (messageEl) {
            messageEl.textContent = form.getAttribute("data-confirm") || "Bu islemi onayliyor musunuz?";
        }
        modal.show();
    });

    approveBtn?.addEventListener("click", function () {
        if (!pendingForm) {
            modal.hide();
            return;
        }

        pendingForm.dataset.confirmed = "1";
        pendingForm.requestSubmit();
        pendingForm = null;
        modal.hide();
    });

    modalEl.addEventListener("hidden.bs.modal", function () {
        pendingForm = null;
    });
}

function initRegisterTypeSwitcher() {
    const patientBtn = document.getElementById("patientBtn");
    const doctorBtn = document.getElementById("doctorBtn");
    const doctorFields = document.getElementById("doctorFields");
    const userTypeInput = document.getElementById("userTypeInput");

    if (!patientBtn || !doctorBtn || !doctorFields || !userTypeInput) {
        return;
    }

    patientBtn.addEventListener("click", function () {
        toggleActiveButton(this, doctorBtn);
        userTypeInput.value = this.dataset.usertype;
        doctorFields.style.display = "none";
    });

    doctorBtn.addEventListener("click", function () {
        toggleActiveButton(this, patientBtn);
        userTypeInput.value = this.dataset.usertype;
        doctorFields.style.display = "block";
    });
}

function toggleActiveButton(activeBtn, inactiveBtn) {
    activeBtn.classList.add("active");
    inactiveBtn.classList.remove("active");
}

function initProfilePhotoPreview() {
    const profilePhotoInput = document.getElementById("profilePhotoInput");
    const profilePhotoPreview = document.getElementById("profilePhotoPreview");

    if (!profilePhotoInput || !profilePhotoPreview) {
        return;
    }

    profilePhotoInput.addEventListener("change", function () {
        const file = this.files && this.files[0];
        if (!file) {
            profilePhotoPreview.style.display = "none";
            profilePhotoPreview.removeAttribute("src");
            return;
        }

        const reader = new FileReader();
        reader.onload = function (e) {
            profilePhotoPreview.src = e.target.result;
            profilePhotoPreview.style.display = "block";
        };
        reader.readAsDataURL(file);
    });
}

function initCertificatePreview() {
    const certificatesInput = document.getElementById("certificatesInput");
    const certificatesPreviewContainer = document.getElementById("certificatesPreview");

    if (!certificatesInput || !certificatesPreviewContainer) {
        return;
    }

    certificatesInput.addEventListener("change", function () {
        certificatesPreviewContainer.innerHTML = "";
        const files = this.files ? Array.from(this.files) : [];
        if (files.length === 0) {
            return;
        }

        files.forEach((file) => {
            const item = document.createElement("div");
            item.className = "certificate-preview-item";
            item.title = file.name;

            if (file.type.startsWith("image/")) {
                const img = document.createElement("img");
                img.alt = file.name;
                img.className = "certificate-preview-thumb";
                item.appendChild(img);

                const reader = new FileReader();
                reader.onload = function (e) {
                    img.src = e.target.result;
                };
                reader.readAsDataURL(file);
            } else {
                const icon = document.createElement("i");
                icon.className = "fas fa-file-pdf certificate-preview-icon";
                item.appendChild(icon);
            }

            const caption = document.createElement("div");
            caption.className = "certificate-preview-name";
            caption.textContent = file.name;
            item.appendChild(caption);
            certificatesPreviewContainer.appendChild(item);
        });
    });
}

function initFloatingLabels() {
    const floatingInputs = document.querySelectorAll(".form-floating-group .form-control");
    floatingInputs.forEach((input) => {
        if (input.value || input.placeholder) {
            input.classList.add("has-content");
        }

        input.addEventListener("input", (e) => {
            if (e.target.value) {
                e.target.classList.add("has-content");
            } else {
                e.target.classList.remove("has-content");
            }
        });
    });
}

function initLocationNoteHistory() {
    const historyInputs = document.querySelectorAll("[data-location-note-history]");
    if (historyInputs.length === 0) {
        return;
    }

    historyInputs.forEach((inputElement) => {
        if (!(inputElement instanceof HTMLInputElement || inputElement instanceof HTMLTextAreaElement)) {
            return;
        }

        const input = inputElement;
        const storageKey = input.getAttribute("data-location-note-history");
        if (!storageKey) {
            return;
        }

        const wrapper = input.closest(".location-note-wrapper") || input.parentElement;
        if (!wrapper) {
            return;
        }

        wrapper.classList.add("location-note-history-host");

        const dropdown = document.createElement("div");
        dropdown.className = "location-note-history-dropdown";
        dropdown.style.display = "none";
        wrapper.appendChild(dropdown);

        const readHistory = () => {
            try {
                const value = localStorage.getItem(storageKey);
                const parsed = value ? JSON.parse(value) : [];
                return Array.isArray(parsed) ? parsed.filter((x) => typeof x === "string" && x.trim().length > 0) : [];
            } catch {
                return [];
            }
        };

        const writeHistory = (items) => {
            localStorage.setItem(storageKey, JSON.stringify(items.slice(0, 12)));
        };

        const saveCurrentValue = () => {
            const text = (input.value || "").trim();
            if (!text) {
                return;
            }

            const existing = readHistory().filter((x) => x.toLowerCase() !== text.toLowerCase());
            existing.unshift(text);
            writeHistory(existing);
        };

        const removeItem = (text) => {
            const existing = readHistory().filter((x) => x !== text);
            writeHistory(existing);
            render(input.value || "");
        };

        const openDropdown = () => {
            dropdown.style.display = "block";
        };

        const closeDropdown = () => {
            dropdown.style.display = "none";
        };

        const render = (filterText) => {
            const normalizedFilter = (filterText || "").trim().toLowerCase();
            const items = readHistory().filter((x) => x.toLowerCase().includes(normalizedFilter));

            dropdown.innerHTML = "";
            if (items.length === 0) {
                const empty = document.createElement("div");
                empty.className = "location-note-history-empty";
                empty.textContent = "Kayitli acik adres yok.";
                dropdown.appendChild(empty);
                return;
            }

            items.forEach((item) => {
                const row = document.createElement("div");
                row.className = "location-note-history-item";

                const textButton = document.createElement("button");
                textButton.type = "button";
                textButton.className = "location-note-history-select";
                textButton.textContent = item;
                textButton.addEventListener("click", function () {
                    input.value = item;
                    closeDropdown();
                });

                const deleteButton = document.createElement("button");
                deleteButton.type = "button";
                deleteButton.className = "location-note-history-delete";
                deleteButton.title = "Adresi listeden sil";
                deleteButton.innerHTML = "<i class='fas fa-times'></i>";
                deleteButton.addEventListener("click", function () {
                    removeItem(item);
                });

                row.appendChild(textButton);
                row.appendChild(deleteButton);
                dropdown.appendChild(row);
            });
        };

        input.addEventListener("focus", function () {
            render(input.value || "");
            openDropdown();
        });

        input.addEventListener("click", function () {
            render(input.value || "");
            openDropdown();
        });

        input.addEventListener("input", function () {
            render(input.value || "");
            openDropdown();
        });

        input.addEventListener("blur", function () {
            window.setTimeout(closeDropdown, 150);
        });

        if (input.form) {
            input.form.addEventListener("submit", saveCurrentValue, true);

            const submitButtons = input.form.querySelectorAll("button[type='submit'], input[type='submit']");
            submitButtons.forEach((button) => {
                button.addEventListener("click", saveCurrentValue);
            });
        }

        input.addEventListener("change", saveCurrentValue);
        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                saveCurrentValue();
            }
        });

        dropdown.addEventListener("mousedown", function (event) {
            event.preventDefault();
        });
    });
}
