# Mentora Frontend Redesign Brief (Google Stitch Input)

## 1) Product Direction

- Product type: mental health appointment and care continuity platform
- Primary users: Doctor (psychologist), Patient
- Core promise: discover, schedule, manage, and continue care safely
- Tone: calm, trusted, clinical-but-human

## 2) Information Architecture

- Public
  - Landing
  - Login
  - Register
  - Forgot password
  - Reset password
- Doctor app
  - Dashboard (appointments)
  - Incoming requests
  - Auto routine manager
  - Clinical notes
  - Profile settings
  - Notification settings
- Patient app
  - Appointment discovery
  - My requests
  - My notes (share/revoke)
  - Profile settings
  - Notification settings

## 3) Screen Contracts (Interaction-Level)

### Doctor Dashboard
- KPI strip: today slots, pending requests, upcoming reserved, completion rate
- Appointment cards: status chip, date/time, types, specialties, price, patient summary
- Actions: requests list, directions, delete
- Conflict state: `CancelledByConflict` card shows warning copy

### Incoming Requests (Doctor)
- Filter: status + optional appointment context
- Request card: patient summary, request note, appointment window
- Approve action: confirm modal and side effect note
  - "Onay durumunda cakisan musait slotlar otomatik kapatilir"
- Reject action: reason required

### Auto Routine Manager
- Routine list with status chips: Active / Paused
- Quick pause presets: 1/3/7/30 day
- Pause-until datetime input
- Create/edit fields:
  - name, weekdays, start time, duration
  - online/in-person
  - fee range
  - specialties
  - active date range
  - notes

### Clinical Notes (Doctor)
- Left: note create form (patient + content)
- Right: timeline
- Shared-note metadata:
  - source label (own/shared)
  - shared by patient + shared date (when applicable)

### Appointment Discovery (Patient)
- Sticky filter rail: doctor, specialty, date, fee, distance, type
- Result cards: doctor identity, rating, slot, price, specialties
- Actions: request, directions, location modal
- Conflict-safe booking warning appears via server response (`TempData` alert)

### My Notes (Patient)
- Selectable note cards
- Share selected notes to doctor
- Revoke selected notes from doctor
- Audit list per note:
  - doctor, shared timestamp
  - revoked timestamp if revoked

### Notification Settings
- Toggles:
  - email enabled
  - appointment reminders
  - request status emails
- Reminder minutes input (10-720)
- SMTP test e-mail form

### Profile Settings
- Editable identity fields:
  - first/last name, phone, birth date, gender, about
- Profile image upload:
  - photo preview
  - validated upload constraints copy
- Location controls:
  - map picker (when API key exists)
  - manual lat/lon fallback
  - clear-location action
- Email confirmation state:
  - confirmed/unconfirmed badge
  - resend confirmation button (unconfirmed users)

### Auth Recovery
- Login:
  - forgot-password CTA
- Forgot password:
  - single e-mail input
  - non-enumerating confirmation copy
- Reset password:
  - token-backed password reset form

## 4) API Mapping (UI -> Backend)

### Appointment & Requests
- `GET /PatientDashboard/Index` -> appointment list/filter
- `POST /AppointmentRequest/CreateRequest` -> create request
- `GET /Request/Index` -> doctor incoming requests
- `POST /Request/Approve` -> approve request
- `POST /Request/Reject` -> reject request
- `GET /Request/MyRequests` -> patient requests
- `POST /Request/Cancel` -> cancel patient pending request

### Automation
- `GET /Automation/Index` -> routine dashboard
- `POST /Automation/Create` -> create routine
- `GET /Automation/Edit/{id}` -> edit page
- `POST /Automation/Edit` -> update routine
- `POST /Automation/Pause` -> pause by days or pause-until
- `POST /Automation/Resume` -> resume routine
- `POST /Automation/Delete` -> delete routine

### Clinical Notes
- `GET /ClinicalNotes/Index` -> doctor note dashboard
- `POST /ClinicalNotes/Create` -> create note
- `GET /ClinicalNotes/MyNotes` -> patient notes + audit
- `POST /ClinicalNotes/Share` -> share notes
- `POST /ClinicalNotes/RevokeShare` -> revoke shares

### Notification & Ops
- `GET /Settings/Notifications` -> get preferences
- `POST /Settings/Notifications` -> update preferences
- `POST /Settings/Notifications/TestEmail` -> enqueue smtp test email
- `GET /health/live` -> liveness
- `GET /health/ready` -> readiness (db)

### Account
- `GET /Account/Profile` -> profile settings screen
- `POST /Account/Profile` -> update profile + location + photo
- `POST /Account/ResendEmailConfirmation` -> enqueue new confirmation mail
- `GET /Account/ForgotPassword` -> forgot password screen
- `POST /Account/ForgotPassword` -> enqueue reset e-mail
- `GET /Account/ResetPassword` -> reset form by token
- `POST /Account/ResetPassword` -> password reset commit
- `GET /Account/ConfirmEmail` -> email confirmation callback

## 5) Visual Language (Intentional, Non-Generic)

- Heading font: Manrope
- Body font: IBM Plex Sans
- Palette: warm neutral surfaces + trust blue accent
- Components:
  - status chips (active/pending/danger/neutral)
  - dense workflow cards
  - clear warning callouts for destructive/conflict flows
- Motion:
  - light card reveal
  - non-blocking state transitions

## 6) Accessibility Baseline

- WCAG AA contrast minimum
- keyboard navigation for all forms/actions
- visible focus ring
- status is never color-only (color + text)

## 7) Acceptance Checklist (Stitch Output Must Satisfy)

- [ ] Doctor dashboard includes status-aware appointment card states
- [ ] Incoming request approve flow has explicit side-effect warning
- [ ] Automation screen supports quick pause presets and pause-until
- [ ] Patient note screen includes share + revoke + audit views
- [ ] Notification settings include test-email UI
- [ ] Profile settings include location/map, image upload, and email status widgets
- [ ] Login flow includes forgot-password and reset-password screens
- [ ] Directions CTA appears consistently on location-enabled cards
- [ ] Mobile behavior defined (filter drawer / stacked cards)
- [ ] Components map cleanly to listed backend endpoints

## 8) Suggested Stitch Prompt

"Design a role-based healthcare scheduling web app called Mentora. Create high-fidelity, production-ready responsive screens for Doctor Dashboard, Incoming Requests, Auto Routine Manager, Clinical Notes, Patient Discovery, Patient Notes Sharing (share + revoke + audit), Notification Settings with SMTP test action, Profile Settings (map/location + avatar + email confirmation state), and Auth Recovery screens (forgot/reset password). Use Manrope + IBM Plex Sans, warm-neutral + trust-blue palette, clear status chips, conflict warning patterns, and map direction actions. Prioritize dense professional workflows and accessibility (WCAG AA)."

## 9) Stitch Generation Outputs (2026-03-29)

- Stitch project: `projects/5773509408620874481` (Mentora - Modern Dashboard)
- Local export folder: `.stitch/designs`

Generated screens:
- Doctor Dashboard (mobile): `projects/5773509408620874481/screens/207a3b982f3f4238b34fb808b194e076`
- Incoming Requests (mobile): `projects/5773509408620874481/screens/3ca3332b2854402eab83fcee15ef5ac2`
- Auto Routine Manager (mobile): `projects/5773509408620874481/screens/e2c17bcd4514440cabd15c9ce31846f0`
- Auto Routine Manager (desktop): `projects/5773509408620874481/screens/9405ec2e79344b79a0aae50458a59df2`
- Clinical Notes (mobile): `projects/5773509408620874481/screens/c2b9cc25cdf5404c8b2281859e8e6f15`
- Patient Discovery (mobile): `projects/5773509408620874481/screens/2a00752ec8ff4a8999c51ae9c04514dd`
- My Notes / Share-Revoke (mobile): `projects/5773509408620874481/screens/3c69f93887514963908172aa74828e77`
- Notification Settings (mobile): `projects/5773509408620874481/screens/a77c721cab024b68bb372b715c2d390f`

Exported files:
- `doctor-dashboard-mobile.(html|png)`
- `incoming-requests-mobile.(html|png)`
- `automation-mobile.(html|png)`
- `automation-desktop.(html|png)`
- `clinical-notes-mobile.(html|png)`
- `patient-discovery-mobile.(html|png)`
- `my-notes-mobile.(html|png)`
- `notification-settings-mobile.(html|png)`
