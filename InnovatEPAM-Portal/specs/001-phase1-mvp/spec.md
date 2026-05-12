# Feature Specification: Phase 1 MVP — InnovatEPAM Portal

**Feature Branch**: `001-phase1-mvp`  
**Created**: 2026-05-12  
**Status**: Draft  
**Input**: User description: "Build Phase 1 MVP of InnovatEPAM Portal. Features include: 1. User Management (Authentication: register/login/logout, Roles: submitter/admin). 2. Idea Submission (Form with title/description/category, single file attachment, idea listing view). 3. Evaluation Workflow (Status tracking: submitted/under review/accepted/rejected, Admin accept/reject with comments)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Submitter captures and tracks an idea (Priority: P1)

A staff member with a Submitter account signs in to the portal, fills out an
idea form (title, description, category), optionally attaches a single
supporting file, and submits the idea. The idea immediately appears in the
public idea listing with status **Submitted**, and the submitter can see it in
their own list and follow its status as it changes.

**Why this priority**: Without idea capture there is no portal. This single
slice — sign in, submit, see it listed — already delivers tangible value:
EPAM staff can record innovation ideas in one place instead of email.

**Independent Test**: A new user registers, logs in, submits an idea with an
attachment, logs out, logs back in, and finds the idea in the listing with the
correct title, category, attachment link, and status **Submitted**.

**Acceptance Scenarios**:

1. **Given** an unauthenticated visitor, **When** they complete the
   registration form with a valid corporate email and password and submit it,
   **Then** their account is created with the Submitter role and they are
   logged in.
2. **Given** a logged-in Submitter on the new-idea form, **When** they enter
   a title, description, and category, attach a permitted file, and submit,
   **Then** the idea is saved with status **Submitted** and a confirmation is
   shown with a link to the idea.
3. **Given** a logged-in Submitter, **When** they open the idea listing,
   **Then** they see at minimum each idea's title, category, submitter name,
   submission date, and current status, sorted with the newest first.
4. **Given** a logged-in user, **When** they click the Logout control,
   **Then** their session ends and protected pages redirect them to login.

---

### User Story 2 - Admin reviews and decides on an idea (Priority: P2)

An Admin signs in, opens the listing filtered to ideas needing review, opens
one, and either accepts or rejects it. The Admin must enter a comment with
the decision; the comment and decision become part of the idea's history and
the idea's status changes to **Accepted** or **Rejected**. The Admin can also
mark an idea as **Under Review** while evaluation is in progress.

**Why this priority**: Capture without disposition is a backlog. This story
turns the portal from a suggestion box into an evaluation workflow and is
required before the MVP can be claimed as "done", but it depends on Story 1
being in place to have anything to act on.

**Independent Test**: With at least one idea already in **Submitted**, an
Admin marks it **Under Review**, then either accepts or rejects it with a
comment. The idea's status, decision comment, and decision timestamp are
visible to both the Admin and the original Submitter.

**Acceptance Scenarios**:

1. **Given** an Admin viewing an idea in status **Submitted**, **When** they
   choose **Move to Under Review**, **Then** the idea's status becomes
   **Under Review** and the change is recorded with timestamp and Admin
   identity.
2. **Given** an Admin viewing an idea in status **Submitted** or **Under
   Review**, **When** they choose **Accept** and enter a non-empty comment,
   **Then** the idea's status becomes **Accepted**, the comment is stored,
   and a decision record is created.
3. **Given** an Admin viewing an idea in status **Submitted** or **Under
   Review**, **When** they choose **Reject** and enter a non-empty comment,
   **Then** the idea's status becomes **Rejected** and the comment is stored.
4. **Given** an Admin attempts to accept or reject without entering a
   comment, **When** they submit the decision, **Then** the system blocks the
   action and prompts them to provide a comment.
5. **Given** a Submitter, **When** they view their own idea after a
   decision, **Then** they can see the new status, the decision comment, and
   who decided.

---

### User Story 3 - Anyone in the portal browses the idea catalog (Priority: P3)

Any signed-in user (Submitter or Admin) can browse the full idea listing,
filter by status and category, and open an idea to see its full description,
attachment, and status history.

**Why this priority**: Visibility builds trust and prevents duplicate
submissions, but the portal still functions as an MVP if filtering is basic.
This story extends Story 1's listing.

**Independent Test**: A user opens the listing, applies a status filter
(e.g., **Accepted**) and a category filter, and sees only matching ideas;
clicking one opens the detail view with description, attachment download, and
status history.

**Acceptance Scenarios**:

1. **Given** a signed-in user on the idea listing, **When** they select a
   status filter, **Then** only ideas with that status are displayed.
2. **Given** a signed-in user on the idea listing, **When** they select a
   category filter, **Then** only ideas in that category are displayed.
3. **Given** a signed-in user opens an idea, **When** the detail view loads,
   **Then** they see title, full description, category, submitter, submission
   date, current status, decision comment (if any), and a link to download
   the attachment when one exists.

---

### Edge Cases

- A submitter uploads a file that exceeds the size limit or has a disallowed
  type → submission is rejected with a clear, specific message and the form
  state is preserved.
- A submitter submits the form with title or description blank → field-level
  validation messages appear and submission is blocked.
- Two Admins open the same idea simultaneously and both attempt a decision →
  the second decision is rejected with a message indicating the idea has
  already been decided, and the current state is shown.
- A user tries to access an Admin-only page (e.g., decide on an idea) while
  signed in as a Submitter → they receive a not-authorized response and are
  redirected to a safe page.
- An unauthenticated visitor opens any portal URL other than register or
  login → they are redirected to login and, after authenticating, returned
  to their intended destination.
- A user submits the registration form with an email that already exists →
  the system reports the conflict without revealing whether the password
  matched.
- The attachment is missing or has been removed when a viewer clicks the
  download link → the viewer sees a clear "attachment not available" message
  rather than a generic error.
- An Admin attempts to change the status of an already-Accepted or
  already-Rejected idea → the action is disabled in the UI and refused by
  the system; status transitions are one-way to a final state.

## Requirements *(mandatory)*

### Functional Requirements

**User Management**

- **FR-001**: The system MUST allow a visitor to register an account by
  providing a name, email, and password, and MUST assign the Submitter role
  by default.
- **FR-002**: The system MUST authenticate registered users with email and
  password and MUST establish an authenticated session on success.
- **FR-003**: The system MUST allow an authenticated user to log out, after
  which their session MUST no longer grant access to protected resources.
- **FR-004**: The system MUST support exactly two roles in Phase 1:
  **Submitter** and **Admin**. Every user MUST have exactly one role.
- **FR-005**: The system MUST enforce that only Admins can perform actions
  reserved for Admins (move to under review, accept, reject) and MUST refuse
  such requests from Submitters.
- **FR-006**: The system MUST hash and salt stored passwords; plaintext
  passwords MUST never be stored or logged.
- **FR-007**: The system MUST enforce a minimum password strength (at least
  8 characters including letters and digits) at registration and password
  change.
- **FR-008**: The system MUST reject registration when the email already
  exists, returning a generic "could not create account" style message
  without disclosing existing-account information.

**Idea Submission**

- **FR-009**: The system MUST provide an authenticated form that accepts an
  idea **title** (required, 5–120 characters), **description** (required, up
  to 4 000 characters), and **category** (required, selected from a
  predefined list).
- **FR-010**: The system MUST allow zero or one file attachment per idea.
  Allowed file types are PDF, PNG, JPG/JPEG, DOCX, PPTX, and XLSX. Maximum
  size is 10 MB.
- **FR-011**: The system MUST persist each submitted idea with: title,
  description, category, attachment reference (if any), submitter identity,
  submission timestamp, and initial status **Submitted**.
- **FR-012**: The system MUST present an idea listing view to every
  authenticated user, showing each idea's title, category, submitter name,
  submission date, and current status, with the most recent ideas first.
- **FR-013**: The system MUST provide an idea detail view showing all stored
  fields, the decision comment (if any), the decision-maker (if any), and a
  link to download the attachment when one exists.
- **FR-014**: The listing MUST support filtering by status and by category
  and MUST be paginated when more than 20 ideas exist.
- **FR-015**: The system MUST scan or content-type-validate uploaded files
  and reject anything not matching the allow-list, regardless of file
  extension.

**Evaluation Workflow**

- **FR-016**: The system MUST track each idea's status as one of:
  **Submitted**, **Under Review**, **Accepted**, **Rejected**.
- **FR-017**: A new idea MUST start in status **Submitted**.
- **FR-018**: An Admin MUST be able to transition an idea from **Submitted**
  to **Under Review**, from **Submitted** or **Under Review** to
  **Accepted**, and from **Submitted** or **Under Review** to **Rejected**.
- **FR-019**: Transitions to **Accepted** or **Rejected** MUST require a
  non-empty Admin comment (1–2 000 characters) recorded with the decision.
- **FR-020**: **Accepted** and **Rejected** are terminal statuses; the
  system MUST refuse further status changes once a terminal status is set.
- **FR-021**: The system MUST record an audit trail entry for every status
  change, capturing prior status, new status, actor, timestamp, and comment
  (when applicable), and MUST display the trail on the idea detail view.
- **FR-022**: The system MUST notify the original submitter (in-portal
  notification at minimum) when their idea changes status.

**Cross-cutting**

- **FR-023**: The system MUST require authentication for every page and
  endpoint except registration, login, and static assets.
- **FR-024**: The system MUST present clear, field-level validation messages
  for every rejected user input.
- **FR-025**: The system MUST log security-relevant events (login success,
  login failure, logout, decision actions) without storing passwords or
  attachment contents in logs.

### Key Entities

- **User**: A person with portal access. Attributes: unique identifier,
  display name, email (unique), password credential (stored hashed), role
  (Submitter | Admin), creation timestamp.
- **Idea**: A submitted innovation proposal. Attributes: unique identifier,
  title, description, category, current status, submitter (User), submission
  timestamp, last-update timestamp, optional attachment reference.
- **Category**: A predefined classification for ideas (e.g., Process,
  Product, Technology, People, Other). Closed list maintained outside the
  Phase 1 UI.
- **Attachment**: An uploaded file associated with at most one Idea.
  Attributes: unique identifier, original filename, content type, size,
  storage reference, upload timestamp.
- **Decision**: An Admin action on an Idea. Attributes: unique identifier,
  Idea reference, actor (User in Admin role), action (Move to Under Review |
  Accept | Reject), comment (required for Accept/Reject), timestamp.
- **StatusHistoryEntry**: Audit record of a status change. Attributes:
  unique identifier, Idea reference, prior status, new status, actor,
  timestamp, optional comment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time Submitter can complete registration, login, and
  submission of a valid idea (with attachment) end-to-end in under 3 minutes
  on a desktop browser.
- **SC-002**: An Admin can locate a submitted idea, open it, and record an
  accept-with-comment decision in under 60 seconds.
- **SC-003**: 95 % of valid idea submissions persist successfully on the
  first attempt under nominal load (100 concurrent users).
- **SC-004**: 100 % of accept/reject decisions made through the Admin UI
  produce a status change, a stored comment, and a corresponding audit-trail
  entry visible to the submitter within 2 seconds.
- **SC-005**: 0 % of Submitter-role accounts can successfully invoke
  Admin-only actions in penetration tests of role enforcement.
- **SC-006**: 90 % of test users in usability evaluation complete their
  first idea submission without help, and 90 % rate the listing view as
  "easy to scan".
- **SC-007**: The MVP supports at least 1 000 stored ideas and 200
  registered users with idea-listing page load times under 1.5 seconds at
  the 95th percentile.

## Assumptions

- Users access the portal from modern desktop and mobile browsers
  (last-two-versions of Chrome, Edge, Firefox, Safari) over reliable
  corporate or home internet connectivity.
- Authentication in Phase 1 is local (email + password). SSO/OAuth and MFA
  are explicitly out of scope and may be added in a later phase.
- Admin accounts are provisioned by an out-of-band process (e.g., promoting
  an existing Submitter via a backend operation); a self-service "request
  admin" flow is out of scope for Phase 1.
- The category list is fixed for Phase 1 and seeded at deployment; a UI for
  managing categories is out of scope.
- File attachments are stored on the same backend the API uses; an
  enterprise content store integration is out of scope for Phase 1.
- Notifications in Phase 1 are in-portal only; email notifications may be
  added in a later phase.
- Internationalization is limited to English in Phase 1.
- Reporting, analytics, comments by non-Admins, voting, and idea editing
  after submission are explicitly out of scope for Phase 1.
- Existing project constitution (`.specify/memory/constitution.md`) governs
  technology choices; the spec does not redefine them.
