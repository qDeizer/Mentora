document.addEventListener('DOMContentLoaded', function () {

    // --- Register Page: User Type Switching ---
    const patientBtn = document.getElementById('patientBtn');
    const doctorBtn = document.getElementById('doctorBtn');
    const doctorFields = document.getElementById('doctorFields');
    const userTypeInput = document.getElementById('userTypeInput');

    if (patientBtn && doctorBtn && doctorFields && userTypeInput) {
        patientBtn.addEventListener('click', function () {
            toggleActiveButton(this, doctorBtn);
            userTypeInput.value = this.dataset.usertype;
            doctorFields.style.display = 'none';
        });

        doctorBtn.addEventListener('click', function () {
            toggleActiveButton(this, patientBtn);
            userTypeInput.value = this.dataset.usertype;
            doctorFields.style.display = 'block';
        });
    }

    function toggleActiveButton(activeBtn, inactiveBtn) {
        activeBtn.classList.add('active');
        inactiveBtn.classList.remove('active');
    }

    // --- Register Page: Image Preview ---
    const profilePhotoInput = document.getElementById('profilePhotoInput');
    const profilePhotoPreview = document.getElementById('profilePhotoPreview');

    if (profilePhotoInput && profilePhotoPreview) {
        profilePhotoInput.addEventListener('change', function () {
            const file = this.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    profilePhotoPreview.src = e.target.result;
                    profilePhotoPreview.style.display = 'block';
                }
                reader.readAsDataURL(file);
            }
        });
    }

    const certificatesInput = document.getElementById('certificatesInput');
    const certificatesPreviewContainer = document.getElementById('certificatesPreview');

    if (certificatesInput && certificatesPreviewContainer) {
        certificatesInput.addEventListener('change', function () {
            certificatesPreviewContainer.innerHTML = ''; // Clear previous previews
            if (this.files) {
                Array.from(this.files).forEach(file => {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        const img = document.createElement('img');
                        img.src = e.target.result;
                        certificatesPreviewContainer.appendChild(img);
                    }
                    reader.readAsDataURL(file);
                });
            }
        });
    }

    // --- Floating Labels Initialization ---
    // Handle floating labels for pre-filled forms or on page load.
    const floatingInputs = document.querySelectorAll('.form-floating-group .form-control');
    floatingInputs.forEach(input => {
        if (input.value || input.placeholder) {
            input.classList.add('has-content');
        }
        input.addEventListener('input', (e) => {
            if (e.target.value) {
                e.target.classList.add('has-content');
            } else {
                e.target.classList.remove('has-content');
            }
        });
    });

});

// Note: The initMap function for Google Maps is placed in the Register.cshtml
// view itself to ensure it's only loaded on that page and has access to the
// Google Maps API script loaded in _Layout.cshtml.