import SwiftUI

struct RegistrationView: View {

    // MARK: - Eingaben

    @State private var email = ""
    @State private var password = ""
    @State private var confirmation = ""

    // MARK: - UI

    @State private var submitted = false
    @State private var isSubmitting = false
    @State private var showSuccessAlert = false
    @State private var registeredEmail = ""
    @State private var requestError: String?

    @FocusState private var focusedField: Field?

    private enum Field: Hashable {
        case email
    }

    // MARK: - Passwortstärke

    private enum PasswordStrength {
        case weak
        case medium
        case strong
    }

    // MARK: - Passwortanforderungen

    private var requirements: [(title: String, met: Bool)] {
        [
            (
                "15+",
                password.utf16.count >= 15
            ),
            (
                "A–Z",
                matches("[A-ZÄÖÜ]")
            ),
            (
                "a–z",
                matches("[a-zäöüß]")
            ),
            (
                "0–9",
                matches("[0-9]")
            ),
            (
                "@#$",
                password.contains { character in
                    "@#$%&*-_!+=:,.?/\"'();".contains(character)
                }
            )
        ]
    }

    private var fulfilledCount: Int {
        requirements.filter { $0.met }.count
    }

    // MARK: - Passwortstärke bestimmen

    private var passwordStrength: PasswordStrength {

        if fulfilledCount == 5 {
            return .strong
        }

        if fulfilledCount >= 3 {
            return .medium
        }

        return .weak
    }

    private var passwordStrengthText: String {

        switch passwordStrength {

        case .weak:
            return "Schwach"

        case .medium:
            return "Mittel"

        case .strong:
            return "Stark"
        }
    }

    private var passwordStrengthColor: Color {

        switch passwordStrength {

        case .weak:
            return .red

        case .medium:
            return .orange

        case .strong:
            return .green
        }
    }

    // MARK: - E-Mail-Validierung

    private var emailError: String? {

        let normalized = email.trimmingCharacters(
            in: .whitespacesAndNewlines
        )

        if normalized.isEmpty {
            return "Bitte gib deine E-Mail-Adresse ein."
        }

        let pattern =
            #"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$"#

        if normalized.range(
            of: pattern,
            options: .regularExpression
        ) == nil {
            return "Bitte gib eine gültige E-Mail-Adresse ein."
        }

        return nil
    }

    // MARK: - Passwortbestätigung

    private var confirmationError: String? {

        if confirmation.isEmpty {
            return "Bitte bestätige dein Passwort."
        }

        if confirmation != password {
            return "Die Passwörter stimmen nicht überein."
        }

        return nil
    }

    // MARK: - Body

    var body: some View {

        ScrollView {

            VStack(
                alignment: .leading,
                spacing: 24
            ) {

                // MARK: Header

                header

                // MARK: E-Mail-Adresse

                VStack(
                    alignment: .leading,
                    spacing: 8
                ) {

                    Text("E-Mail-Adresse")
                        .font(.subheadline)
                        .fontWeight(.semibold)

                    HStack(spacing: 12) {

                        Image(systemName: "envelope.fill")
                            .font(
                                .system(
                                    size: 17,
                                    weight: .medium
                                )
                            )
                            .foregroundStyle(
                                focusedField == .email
                                    ? Color.accentColor
                                    : Color.secondary
                            )
                            .frame(width: 22)
                            .accessibilityHidden(true)

                        // MARK: E-Mail Placeholder
                        // name@beispiel.de wird jetzt hellgrau
                        // dargestellt und sieht nicht wie
                        // eine eingegebene E-Mail-Adresse aus.

                        TextField(
                            "",
                            text: $email,
                            prompt:
                                Text("E-Mail-Adresse")
                                .foregroundStyle(
                                    Color.secondary.opacity(0.45)
                                )
                        )
                        .keyboardType(.emailAddress)
                        .textContentType(.emailAddress)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .focused(
                            $focusedField,
                            equals: .email
                        )
                        .submitLabel(.next)
                        .accessibilityLabel(
                            "E-Mail-Adresse"
                        )
                    }

                    .padding(.horizontal, 16)

                    .padding(.vertical, 12)
                    .frame(minHeight: 56)

                    .background {

                        RoundedRectangle(
                            cornerRadius: 14
                        )
                        .fill(
                            Color(
                                uiColor: .systemBackground
                            )
                        )
                    }

                    .overlay {

                        RoundedRectangle(
                            cornerRadius: 14
                        )
                        .stroke(
                            emailBorderColor,
                            lineWidth:
                                focusedField == .email
                                    ? 2
                                    : 1
                        )
                    }

                    .shadow(
                        color:
                            focusedField == .email
                                ? Color.black.opacity(0.14)
                                : Color.black.opacity(0.05),

                        radius:
                            focusedField == .email
                                ? 10
                                : 4,

                        y:
                            focusedField == .email
                                ? 6
                                : 2
                    )

                    .offset(
                        y:
                            focusedField == .email
                                ? -2
                                : 0
                    )

                    .animation(
                        .easeOut(duration: 0.18),
                        value: focusedField
                    )

                    if submitted,
                       let emailError {

                        errorMessage(emailError)
                    }
                }

                // MARK: - Premium Passwort

                VStack(
                    alignment: .leading,
                    spacing: 12
                ) {

                    PremiumPasswordField(
                        value: $password,
                        strengthColor: passwordStrengthColor
                    )

                    // MARK: Passwortstärke

                    passwordStrengthView

                    // MARK: Fehler

                    if submitted &&
                        fulfilledCount < requirements.count {

                        errorMessage(
                            "Bitte erfülle alle fünf Passwortanforderungen."
                        )
                    }
                }

                // MARK: - Passwort bestätigen

                VStack(
                    alignment: .leading,
                    spacing: 8
                ) {

                    RegistrationPasswordField(
                        title: "Passwort bestätigen",
                        placeholder: "Passwort bestätigen",
                        value: $confirmation
                    )

                    if submitted,
                       let confirmationError {

                        errorMessage(
                            confirmationError
                        )
                    }
                }

                // MARK: - Konto erstellen

                Button {

                    dismissKeyboard()
                    validate()

                } label: {

                    HStack(spacing: 8) {

                        Text(isSubmitting ? "Konto wird erstellt …" : "Konto erstellen")
                            .fontWeight(.semibold)

                        Image(
                            systemName: "arrow.right"
                        )
                        .font(
                            .system(
                                size: 14,
                                weight: .semibold
                            )
                        )
                    }

                    .foregroundStyle(.white)

                    .frame(
                        maxWidth: .infinity
                    )

                    .padding(.vertical, 12)
                    .frame(minHeight: 54)

                    .background(
                        Color.accentColor
                    )

                    .clipShape(
                        RoundedRectangle(
                            cornerRadius: 14
                        )
                    )

                    .shadow(
                        color:
                            Color.accentColor.opacity(0.22),
                        radius: 8,
                        y: 4
                    )
                }

                .buttonStyle(.plain)

                if isSubmitting {
                    ProgressView("Registrierung läuft")
                        .frame(maxWidth: .infinity)
                }

                if let requestError {
                    errorMessage(requestError)
                }
            }

            .frame(maxWidth: 480)

            .frame(
                maxWidth: .infinity
            )

            .padding(24)
        }

        // MARK: - Hintergrund

        .background(
            Color(
                uiColor: .systemGroupedBackground
            )
            .ignoresSafeArea()
        )
        
        .disabled(isSubmitting)
        .navigationBarBackButtonHidden(isSubmitting)

        // MARK: - Tastatur/Fokus schließen
        //
        // Wenn du auf einen freien Bereich tippst,
        // verlieren E-Mail, Passwort und
        // Passwort bestätigen den Fokus.

        .contentShape(Rectangle())

        .onTapGesture {
            dismissKeyboard()
        }

        // MARK: - Navigation

        .navigationTitle(
            "Registrieren"
        )

        .navigationBarTitleDisplayMode(
            .inline
        )

        .scrollDismissesKeyboard(
            .interactively
        )

        // MARK: - Alert

        .alert(
            "Konto erstellt",
            isPresented: $showSuccessAlert
        ) {
            Button("OK", role: .cancel) { }
        } message: {
            Text("Das Konto für \(registeredEmail) wurde erfolgreich erstellt.")
        }
    }

    // MARK: =====================================================
    // MARK: - Header
    // MARK: =====================================================

    private var header: some View {

        VStack(spacing: 12) {

            Image(
                systemName: "graduationcap.fill"
            )

            .font(
                .system(
                    size: 28,
                    weight: .semibold
                )
            )

            .foregroundStyle(.tint)

            .frame(
                width: 64,
                height: 64
            )

            .background(
                Color.accentColor.opacity(0.12),
                in: RoundedRectangle(
                    cornerRadius: 18
                )
            )

            .accessibilityHidden(true)

            Text("Neues Konto")
                .font(.title.bold())

            Text(
                "Organisiere dein Studium an einem Ort."
            )

            .font(.subheadline)

            .foregroundStyle(.secondary)
        }

        .multilineTextAlignment(.center)

        .frame(
            maxWidth: .infinity
        )
    }

    // MARK: =====================================================
    // MARK: - Passwortstärke
    // MARK: =====================================================

    private var passwordStrengthView: some View {

        VStack(alignment: .leading, spacing: 14) {

            // MARK: - Passwortstärke + Status

            HStack {

                Text("Anforderungen")
                    .font(.system(size: 17, weight: .medium))
                    .foregroundStyle(.primary)

                Spacer()

                Text("\(fulfilledCount) von 5 erfüllt")
                    .font(.system(size: 16, weight: .semibold))
                    .foregroundStyle(passwordStrengthColor)
            }

            // MARK: - Fortschrittsbalken

            GeometryReader { geometry in

                ZStack(alignment: .leading) {

                    Capsule()
                        .fill(
                            Color.secondary.opacity(0.12)
                        )

                    Capsule()
                        .fill(passwordStrengthColor)
                        .frame(
                            width:
                                geometry.size.width
                                * (
                                    Double(fulfilledCount)
                                    / Double(requirements.count)
                                )
                        )
                        .animation(
                            .easeInOut(duration: 0.25),
                            value: fulfilledCount
                        )
                }
            }
            .frame(height: 5)

            // MARK: - Anforderungen

            HStack(spacing: 0) {

                ForEach(
                    requirements,
                    id: \.title
                ) { requirement in

                    HStack(spacing: 6) {

                        Image(
                            systemName:
                                requirement.met
                                    ? "checkmark.circle.fill"
                                    : "circle.fill"
                        )
                        .font(.system(size: 16))
                        .foregroundStyle(
                            requirement.met
                                ? Color.green
                                : Color.secondary.opacity(0.22)
                        )

                        Text(requirement.title)
                            .font(
                                .system(
                                    size: 15,
                                    weight: .regular
                                )
                            )
                            .foregroundStyle(
                                requirement.met
                                    ? Color.green
                                    : Color.secondary
                            )
                            .lineLimit(1)
                    }

                    if requirement.title != requirements.last?.title {
                        Spacer(minLength: 5)
                    }
                }
            }

            // MARK: - Sonderzeichen

            Text(
                "Sonderzeichen: @ # $ % & * - _ ! + = : , . ? / \" ' ( ) ;"
            )
            .font(.system(size: 13))
            .foregroundStyle(
                Color.secondary.opacity(0.85)
            )
            .fixedSize(
                horizontal: false,
                vertical: true
            )
        }
    }

    // MARK: - Einzelne Anforderung

    private func strengthRequirement(
        _ title: String,
        met: Bool
    ) -> some View {

        HStack(spacing: 4) {

            Image(
                systemName:
                    met
                        ? "checkmark.circle.fill"
                        : "circle"
            )

            .font(
                .system(size: 14)
            )

            Text(title)

                .font(.caption)

                .fontWeight(.medium)

                .lineLimit(1)
        }

        .foregroundStyle(
            met
                ? Color.green
                : Color.secondary
        )

        .accessibilityLabel(
            "\(title): "
            + (
                met
                    ? "erfüllt"
                    : "noch nicht erfüllt"
            )
        )
    }

    // MARK: =====================================================
    // MARK: - E-Mail-Rahmen
    // MARK: =====================================================

    private var emailBorderColor: Color {

        if submitted &&
            emailError != nil {

            return .red
        }

        if focusedField == .email {

            return .accentColor
        }

        return Color.secondary.opacity(0.18)
    }

    // MARK: =====================================================
    // MARK: - Fehlermeldung
    // MARK: =====================================================

    private func errorMessage(
        _ message: String
    ) -> some View {

        Label(
            message,
            systemImage:
                "exclamationmark.circle.fill"
        )

        .font(.footnote)

        .foregroundStyle(.red)

        .fixedSize(
            horizontal: false,
            vertical: true
        )
    }

    // MARK: =====================================================
    // MARK: - Regex
    // MARK: =====================================================

    private func matches(
        _ pattern: String
    ) -> Bool {

        password.range(
            of: pattern,
            options: .regularExpression
        ) != nil
    }

    // MARK: =====================================================
    // MARK: - Tastatur und Fokus schließen
    // MARK: =====================================================

    private func dismissKeyboard() {

        // Fokus des E-Mail-Feldes entfernen

        focusedField = nil

        // Auch den First Responder der untergeordneten
        // Passwortfelder entfernen.

        UIApplication.shared.sendAction(
            #selector(UIResponder.resignFirstResponder),
            to: nil,
            from: nil,
            for: nil
        )
    }

    // MARK: =====================================================
    // MARK: - Validierung
    // MARK: =====================================================

    @MainActor
    private func validate() {
        guard !isSubmitting else { return }

        requestError = nil
        email = email.trimmingCharacters(in: .whitespacesAndNewlines)
        submitted = true

        guard emailError == nil,
              fulfilledCount == requirements.count,
              confirmationError == nil else {
            return
        }

        dismissKeyboard()
        isSubmitting = true

        let submittedEmail = email
        let submittedPassword = password

        Task { @MainActor in
            defer { isSubmitting = false }

            do {
                let result = try await RegistrationService().register(
                    email: submittedEmail,
                    password: submittedPassword
                )

                registeredEmail = result.email
                email = ""
                password = ""
                confirmation = ""
                submitted = false
                showSuccessAlert = true
            } catch is CancellationError {
                requestError = "Die Anfrage wurde abgebrochen. "
                    + "Das Konto wurde möglicherweise bereits erstellt."
            } catch {
                requestError = error.localizedDescription
            }
        }
    }
}


// MARK: =========================================================
// MARK: - Premium Passwortfeld
// MARK: =========================================================

private struct PremiumPasswordField: View {

    @Binding var value: String

    let strengthColor: Color

    @State private var isVisible = false

    @FocusState private var isFocused: Bool

    // Passwort steht oben,
    // wenn das Feld aktiv ist
    // oder bereits Text enthält.

    private var showsFloatingLabel: Bool {

        isFocused || !value.isEmpty
    }

    var body: some View {

        ZStack(
            alignment: .topLeading
        ) {

            // MARK: - Feld

            HStack(spacing: 12) {
                
                if !showsFloatingLabel {
                    
                    Image(systemName: "lock.fill").font(.system(size: 17,weight: .medium)).foregroundStyle(.secondary).frame(width: 22).accessibilityHidden(true)
                }

                Group {

                    if isVisible {

                        TextField(
                            showsFloatingLabel
                                ? ""
                                : "Passwort",
                            text: $value
                        )

                    } else {

                        SecureField(
                            showsFloatingLabel
                                ? ""
                                : "Passwort",
                            text: $value
                        )
                    }
                }

                .textContentType(
                    .newPassword
                )

                .textInputAutocapitalization(
                    .never
                )

                .autocorrectionDisabled()

                .focused($isFocused)

                .accessibilityLabel(
                    "Passwort"
                )

                Spacer(
                    minLength: 8
                )

                // MARK: - Auge

                Button {

                    isVisible.toggle()

                    isFocused = true

                } label: {

                    Image(
                        systemName:
                            isVisible
                                ? "eye.slash"
                                : "eye"
                    )

                    .font(
                        .system(
                            size: 17,
                            weight: .medium
                        )
                    )

                    .foregroundStyle(
                        .secondary
                    )

                    .frame(
                        width: 32,
                        height: 32
                    )
                }

                .buttonStyle(.plain)

                .accessibilityLabel(
                    isVisible
                        ? "Passwort ausblenden"
                        : "Passwort anzeigen"
                )
            }

            .padding(
                .horizontal,
                18
            )

            .padding(.vertical, 12)
            .frame(minHeight: 64)

            .background {

                RoundedRectangle(
                    cornerRadius: 16
                )

                .fill(
                    Color(
                        uiColor: .systemBackground
                    )
                )
            }

            // MARK: Rahmen

            .overlay {

                RoundedRectangle(
                    cornerRadius: 16
                )

                .stroke(
                    borderColor,

                    lineWidth:
                        isFocused
                            ? 2
                            : 1
                )
            }

            // MARK: Schatten

            .shadow(
                color:
                    isFocused
                        ? Color.black.opacity(0.10)
                        : Color.black.opacity(0.04),

                radius:
                    isFocused
                        ? 8
                        : 4,

                y:
                    isFocused
                        ? 4
                        : 2
            )

            // MARK: - Floating Label

            if showsFloatingLabel {

                Text("Passwort")

                    .font(
                        .system(
                            size: 14,
                            weight: .semibold
                        )
                    )

                    .foregroundStyle(
                        floatingLabelColor
                    )

                    .padding(
                        .horizontal,
                        6
                    )

                    // Weiß wie das Feld.
                    // Dadurch entsteht die Unterbrechung
                    // im Rahmen wie im Referenzdesign.

                    .background(
                        Color(
                            uiColor: .systemBackground
                        )
                    )

                    .offset(
                        x: 24,
                        y: -9
                    )

                    .transition(
                        .opacity
                    )
            }
        }

        .padding(
            .top,
            showsFloatingLabel
                ? 10
                : 0
        )

        .animation(
            .easeOut(
                duration: 0.18
            ),
            value: showsFloatingLabel
        )

        .animation(
            .easeOut(
                duration: 0.18
            ),
            value: strengthColor
        )
    }

    // MARK: - Rahmenfarbe

    private var borderColor: Color {

        // Leer + nicht aktiv
        // -> neutral

        if !isFocused &&
            value.isEmpty {

            return Color.secondary.opacity(0.20)
        }

        // Leer + aktiv
        // -> blau

        if isFocused &&
            value.isEmpty {

            return .accentColor
        }

        // Passwort vorhanden
        // -> Rot / Orange / Grün

        return strengthColor
    }

    // MARK: - Label-Farbe

    private var floatingLabelColor: Color {

        if value.isEmpty {

            return .accentColor
        }

        return strengthColor
    }
}


// MARK: =========================================================
// MARK: - Passwort bestätigen
// MARK: =========================================================

private struct RegistrationPasswordField: View {

    let title: String

    let placeholder: String

    @Binding var value: String

    @State private var isVisible = false

    @FocusState private var isFocused: Bool

    var body: some View {

        VStack(
            alignment: .leading,
            spacing: 8
        ) {

            Text(title)

                .font(.subheadline)

                .fontWeight(.semibold)

            HStack(spacing: 12) {

                // MARK: Schloss

                Image(
                    systemName: "lock.fill"
                )

                .font(
                    .system(
                        size: 17,
                        weight: .medium
                    )
                )

                .foregroundStyle(
                    isFocused
                        ? Color.accentColor
                        : Color.secondary
                )

                .frame(width: 22)

                .accessibilityHidden(true)

                // MARK: Eingabe

                Group {

                    if isVisible {

                        TextField(
                            placeholder,
                            text: $value
                        )

                    } else {

                        SecureField(
                            placeholder,
                            text: $value
                        )
                    }
                }

                .textContentType(
                    .newPassword
                )

                .textInputAutocapitalization(
                    .never
                )

                .autocorrectionDisabled()

                .focused(
                    $isFocused
                )

                .accessibilityLabel(
                    title
                )

                // MARK: Auge

                Button {

                    isVisible.toggle()

                    isFocused = true

                } label: {

                    Image(
                        systemName:
                            isVisible
                                ? "eye.slash"
                                : "eye"
                    )

                    .font(
                        .system(
                            size: 17,
                            weight: .medium
                        )
                    )

                    .foregroundStyle(
                        .secondary
                    )

                    .frame(
                        width: 32,
                        height: 32
                    )
                }

                .buttonStyle(.plain)

                .accessibilityLabel(
                    "\(title) "
                    + (
                        isVisible
                            ? "ausblenden"
                            : "anzeigen"
                    )
                )
            }

            .padding(
                .horizontal,
                16
            )

            .padding(.vertical, 12)
            .frame(minHeight: 56)

            .background {

                RoundedRectangle(
                    cornerRadius: 14
                )

                .fill(
                    Color(
                        uiColor: .systemBackground
                    )
                )
            }

            .overlay {

                RoundedRectangle(
                    cornerRadius: 14
                )

                .stroke(
                    isFocused
                        ? Color.accentColor
                        : Color.secondary.opacity(0.18),

                    lineWidth:
                        isFocused
                            ? 2
                            : 1
                )
            }

            .shadow(
                color:
                    isFocused
                        ? Color.black.opacity(0.14)
                        : Color.black.opacity(0.05),

                radius:
                    isFocused
                        ? 10
                        : 4,

                y:
                    isFocused
                        ? 6
                        : 2
            )

            .offset(
                y:
                    isFocused
                        ? -2
                        : 0
            )

            .animation(
                .easeOut(
                    duration: 0.18
                ),
                value: isFocused
            )
        }
    }
}


// MARK: =========================================================
// MARK: - Preview
// MARK: =========================================================

#Preview {

    NavigationStack {

        RegistrationView()
    }
}
