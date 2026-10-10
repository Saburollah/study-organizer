import SwiftUI

struct EditProfileView: View {

    let session: AuthSession

    @Environment(\.dismiss)
    private var dismiss

    @EnvironmentObject
    private var sessionManager: SessionManager

    @State
    private var firstName = ""

    @State
    private var lastName = ""

    @State
    private var selectedBirthDate: Date?

    @State
    private var selectedGender: Gender =
        .none

    @State
    private var showDatePicker = false

    @State
    private var isLoading = true

    @State
    private var isSaving = false

    @State
    private var errorMessage: String?

    @State
    private var showSuccessAlert = false


    private enum Gender:
        String,
        CaseIterable,
        Identifiable {

        case none = "Keine Auswahl"
        case male = "Männlich"
        case female = "Weiblich"
        case preferNotToSay = "Keine Angabe"


        var id: Self {
            self
        }


        var apiValue: String? {

            switch self {

            case .none:
                return nil

            case .male:
                return "Male"

            case .female:
                return "Female"

            case .preferNotToSay:
                return "PreferNotToSay"
            }
        }


        static func fromAPI(
            _ value: String?
        ) -> Gender {

            switch value {

            case "Male":
                return .male

            case "Female":
                return .female

            case "PreferNotToSay":
                return .preferNotToSay

            default:
                return .none
            }
        }
    }


    var body: some View {

        ScrollView {

            VStack(
                alignment: .leading,
                spacing: 22
            ) {

                header


                if isLoading {

                    loadingView

                } else {

                    personalDataCard


                    if let errorMessage {

                        errorView(
                            errorMessage
                        )
                    }


                    saveButton
                }
            }
            .frame(
                maxWidth: 500
            )
            .frame(
                maxWidth: .infinity
            )
            .padding(
                .horizontal,
                20
            )
            .padding(
                .top,
                12
            )
            .padding(
                .bottom,
                40
            )
        }
        .background {

            LinearGradient(
                colors: [
                    Color(
                        uiColor:
                            .systemGroupedBackground
                    ),
                    Color.accentColor.opacity(
                        0.025
                    ),
                    Color(
                        uiColor:
                            .systemGroupedBackground
                    )
                ],
                startPoint:
                    .top,
                endPoint:
                    .bottom
            )
            .ignoresSafeArea()
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(
            .inline
        )
        .toolbar {

            ToolbarItem(
                placement:
                    .topBarLeading
            ) {

                Button(
                    "Abbrechen"
                ) {

                    dismiss()
                }
                .font(
                    .system(
                        size: 15,
                        weight: .medium
                    )
                )
                .disabled(
                    isSaving
                )
            }


            ToolbarItem(
                placement:
                    .principal
            ) {

                Text(
                    "Persönliche Daten"
                )
                .font(
                    .system(
                        size: 17,
                        weight: .semibold
                    )
                )
            }
        }
        .sheet(
            isPresented:
                $showDatePicker
        ) {

            birthDatePickerSheet
                .presentationDetents(
                    [.height(390)]
                )
        }
        .alert(
            "Profil gespeichert",
            isPresented:
                $showSuccessAlert
        ) {

            Button(
                "OK",
                role:
                    .cancel
            ) {

                dismiss()
            }

        } message: {

            Text(
                "Deine persönlichen Daten wurden erfolgreich gespeichert."
            )
        }
        .task {

            await loadProfile()
        }
    }


    // MARK: - Header

    private var header: some View {

        VStack(
            spacing: 10
        ) {

            ZStack {

                RoundedRectangle(
                    cornerRadius: 18,
                    style: .continuous
                )
                .fill(
                    LinearGradient(
                        colors: [
                            Color.accentColor.opacity(
                                0.18
                            ),
                            Color.accentColor.opacity(
                                0.08
                            )
                        ],
                        startPoint:
                            .topLeading,
                        endPoint:
                            .bottomTrailing
                    )
                )
                .frame(
                    width: 54,
                    height: 54
                )


                RoundedRectangle(
                    cornerRadius: 18,
                    style: .continuous
                )
                .stroke(
                    Color.white.opacity(
                        0.45
                    ),
                    lineWidth: 0.8
                )
                .frame(
                    width: 54,
                    height: 54
                )


                Image(
                    systemName:
                        "person.fill"
                )
                .font(
                    .system(
                        size: 23,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    Color.accentColor
                )
            }


            Text(
                "Dein Profil"
            )
            .font(
                .system(
                    size: 24,
                    weight: .bold
                )
            )


            Text(
                "Verwalte deine persönlichen Angaben."
            )
            .font(
                .system(
                    size: 14
                )
            )
            .foregroundStyle(
                .secondary
            )
        }
        .frame(
            maxWidth: .infinity
        )
    }


    // MARK: - Loading

    private var loadingView: some View {

        VStack(
            spacing: 12
        ) {

            ProgressView()
                .controlSize(
                    .large
                )


            Text(
                "Profildaten werden geladen …"
            )
            .font(
                .subheadline
            )
            .foregroundStyle(
                .secondary
            )
        }
        .frame(
            maxWidth: .infinity
        )
        .padding(
            .vertical,
            60
        )
    }


    // MARK: - Personal Data Card

    private var personalDataCard: some View {

        VStack(
            spacing: 0
        ) {

            editableTextRow(
                icon:
                    "person.fill",
                title:
                    "Vorname",
                placeholder:
                    "Dein Vorname",
                text:
                    $firstName
            )


            divider


            editableTextRow(
                icon:
                    "person.fill",
                title:
                    "Nachname",
                placeholder:
                    "Dein Nachname",
                text:
                    $lastName
            )


            divider


            emailRow


            divider


            birthDateRow


            divider


            genderRow
        }
        .background(
            premiumCardBackground
        )
    }


    // MARK: - Editable Row

    private func editableTextRow(
        icon: String,
        title: String,
        placeholder: String,
        text: Binding<String>
    ) -> some View {

        HStack(
            spacing: 13
        ) {

            rowIcon(
                icon
            )


            VStack(
                alignment: .leading,
                spacing: 3
            ) {

                Text(
                    title
                )
                .font(
                    .system(
                        size: 12.5,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
                )


                TextField(
                    placeholder,
                    text:
                        text
                )
                .font(
                    .system(
                        size: 16,
                        weight: .medium
                    )
                )
                .textInputAutocapitalization(
                    .words
                )
                .autocorrectionDisabled()
            }


            Spacer()
        }
        .padding(
            .horizontal,
            16
        )
        .frame(
            minHeight: 68
        )
    }


    // MARK: - Email

    private var emailRow: some View {

        HStack(
            spacing: 13
        ) {

            rowIcon(
                "envelope.fill"
            )


            VStack(
                alignment: .leading,
                spacing: 3
            ) {

                Text(
                    "E-Mail-Adresse"
                )
                .font(
                    .system(
                        size: 12.5,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
                )


                Text(
                    session.email
                )
                .font(
                    .system(
                        size: 16,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .primary
                )
                .lineLimit(
                    1
                )
            }


            Spacer()


            Image(
                systemName:
                    "lock.fill"
            )
            .font(
                .system(
                    size: 12
                )
            )
            .foregroundStyle(
                .tertiary
            )
        }
        .padding(
            .horizontal,
            16
        )
        .frame(
            minHeight: 68
        )
    }


    // MARK: - Birth Date

    private var birthDateRow: some View {

        Button {

            showDatePicker =
                true

        } label: {

            HStack(
                spacing: 13
            ) {

                rowIcon(
                    "calendar"
                )


                VStack(
                    alignment: .leading,
                    spacing: 3
                ) {

                    Text(
                        "Geburtsdatum"
                    )
                    .font(
                        .system(
                            size: 12.5,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(
                        .secondary
                    )


                    Text(
                        birthDateText
                    )
                    .font(
                        .system(
                            size: 16,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(
                        selectedBirthDate == nil
                            ? Color.secondary
                            : Color.primary
                    )
                }


                Spacer()


                Image(
                    systemName:
                        "chevron.right"
                )
                .font(
                    .system(
                        size: 13,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    .tertiary
                )
            }
            .padding(
                .horizontal,
                16
            )
            .frame(
                minHeight: 68
            )
        }
        .buttonStyle(
            .plain
        )
    }


    // MARK: - Gender

    private var genderRow: some View {

        Menu {

            ForEach(
                Gender.allCases
            ) { gender in

                Button {

                    selectedGender =
                        gender

                } label: {

                    if selectedGender
                        == gender {

                        Label(
                            gender.rawValue,
                            systemImage:
                                "checkmark"
                        )

                    } else {

                        Text(
                            gender.rawValue
                        )
                    }
                }
            }

        } label: {

            HStack(
                spacing: 13
            ) {

                rowIcon(
                    "person.2.fill"
                )


                VStack(
                    alignment: .leading,
                    spacing: 3
                ) {

                    Text(
                        "Geschlecht"
                    )
                    .font(
                        .system(
                            size: 12.5,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(
                        .secondary
                    )


                    Text(
                        selectedGender.rawValue
                    )
                    .font(
                        .system(
                            size: 16,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(
                        selectedGender == .none
                            ? Color.secondary
                            : Color.primary
                    )
                }


                Spacer()


                Image(
                    systemName:
                        "chevron.up.chevron.down"
                )
                .font(
                    .system(
                        size: 11,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    .tertiary
                )
            }
            .padding(
                .horizontal,
                16
            )
            .frame(
                minHeight: 68
            )
        }
        .buttonStyle(
            .plain
        )
    }


    // MARK: - Date Picker Sheet

    private var birthDatePickerSheet: some View {

        NavigationStack {

            VStack {

                DatePicker(
                    "Geburtsdatum",
                    selection:
                        Binding(
                            get: {

                                selectedBirthDate
                                    ?? defaultBirthDate
                            },
                            set: {

                                selectedBirthDate =
                                    $0
                            }
                        ),
                    in:
                        ...Date(),
                    displayedComponents:
                        .date
                )
                .datePickerStyle(
                    .graphical
                )
                .padding()
            }
            .navigationTitle(
                "Geburtsdatum"
            )
            .navigationBarTitleDisplayMode(
                .inline
            )
            .toolbar {

                ToolbarItem(
                    placement:
                        .topBarLeading
                ) {

                    Button(
                        "Löschen"
                    ) {

                        selectedBirthDate =
                            nil

                        showDatePicker =
                            false
                    }
                }


                ToolbarItem(
                    placement:
                        .topBarTrailing
                ) {

                    Button(
                        "Fertig"
                    ) {

                        if selectedBirthDate == nil {

                            selectedBirthDate =
                                defaultBirthDate
                        }


                        showDatePicker =
                            false
                    }
                }
            }
        }
    }


    // MARK: - Save Button

    private var saveButton: some View {

        Button {

            saveProfile()

        } label: {

            HStack(
                spacing: 8
            ) {

                if isSaving {

                    ProgressView()
                        .tint(
                            .white
                        )


                    Text(
                        "Wird gespeichert …"
                    )

                } else {

                    Text(
                        "Profil speichern"
                    )


                    Image(
                        systemName:
                            "checkmark"
                    )
                }
            }
            .font(
                .system(
                    size: 16,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                .white
            )
            .frame(
                maxWidth: .infinity
            )
            .frame(
                height: 54
            )
            .background(
                Color.accentColor,
                in:
                    RoundedRectangle(
                        cornerRadius: 14,
                        style: .continuous
                    )
            )
            .shadow(
                color:
                    Color.accentColor.opacity(
                        0.20
                    ),
                radius: 8,
                y: 4
            )
        }
        .buttonStyle(
            .plain
        )
        .disabled(
            isSaving
            || isLoading
        )
    }


    // MARK: - Row Icon

    private func rowIcon(
        _ systemName: String
    ) -> some View {

        Image(
            systemName:
                systemName
        )
        .font(
            .system(
                size: 15,
                weight: .semibold
            )
        )
        .foregroundStyle(
            Color.accentColor
        )
        .frame(
            width: 34,
            height: 34
        )
        .background(
            LinearGradient(
                colors: [
                    Color.accentColor.opacity(
                        0.16
                    ),
                    Color.accentColor.opacity(
                        0.07
                    )
                ],
                startPoint:
                    .topLeading,
                endPoint:
                    .bottomTrailing
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 9,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 9,
                style: .continuous
            )
            .stroke(
                Color.white.opacity(
                    0.35
                ),
                lineWidth: 0.7
            )
        }
        .shadow(
            color:
                Color.accentColor.opacity(
                    0.08
                ),
            radius: 4,
            y: 2
        )
    }


    // MARK: - Premium Card

    private var premiumCardBackground: some View {

        RoundedRectangle(
            cornerRadius: 20,
            style: .continuous
        )
        .fill(
            Color(
                uiColor:
                    .systemBackground
            )
            .opacity(
                0.92
            )
        )
        .background {

            RoundedRectangle(
                cornerRadius: 20,
                style: .continuous
            )
            .fill(
                .ultraThinMaterial
            )
        }
        .overlay {

            RoundedRectangle(
                cornerRadius: 20,
                style: .continuous
            )
            .stroke(
                LinearGradient(
                    colors: [
                        Color.white.opacity(
                            0.60
                        ),
                        Color.secondary.opacity(
                            0.08
                        )
                    ],
                    startPoint:
                        .topLeading,
                    endPoint:
                        .bottomTrailing
                ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.black.opacity(
                    0.05
                ),
            radius: 12,
            y: 7
        )
    }


    // MARK: - Divider

    private var divider: some View {

        Divider()
            .padding(
                .leading,
                63
            )
    }


    // MARK: - Birth Date Text

    private var birthDateText: String {

        guard
            let selectedBirthDate

        else {

            return "Nicht angegeben"
        }


        return selectedBirthDate.formatted(
            .dateTime
                .day()
                .month(
                    .wide
                )
                .year()
                .locale(
                    Locale(
                        identifier:
                            "de_DE"
                    )
                )
        )
    }


    // MARK: - Default Date

    private var defaultBirthDate: Date {

        Calendar.current.date(
            byAdding:
                .year,
            value:
                -20,
            to:
                Date()
        )
        ?? Date()
    }


    // MARK: - Error

    private func errorView(
        _ message: String
    ) -> some View {

        Label(
            message,
            systemImage:
                "exclamationmark.circle.fill"
        )
        .font(
            .footnote
        )
        .foregroundStyle(
            .red
        )
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
        .padding(
            14
        )
        .background(
            Color.red.opacity(
                0.07
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 13,
                    style: .continuous
                )
        )
    }


    // MARK: - Load Profile

    @MainActor
    private func loadProfile() async {

        guard
            !session.accessToken.isEmpty

        else {

            return
        }


        isLoading =
            true

        errorMessage =
            nil


        defer {

            isLoading =
                false
        }


        do {

            let profile =
                try await ProfileService()
                    .getProfile(
                        accessToken:
                            session.accessToken
                    )


            firstName =
                profile.firstName
                ?? ""


            lastName =
                profile.lastName
                ?? ""


            selectedBirthDate =
                parseAPIDate(
                    profile.dateOfBirth
                )


            selectedGender =
                Gender.fromAPI(
                    profile.gender
                )


        } catch ProfileServiceError.unauthorized {

            sessionManager.invalidateSession(
                accessToken:
                    session.accessToken
            )


        } catch is CancellationError {

            return


        } catch {

            errorMessage =
                error.localizedDescription
        }
    }


    // MARK: - Save Profile

    @MainActor
    private func saveProfile() {

        guard !isSaving else {

            return
        }


        errorMessage =
            nil

        isSaving =
            true


        let submittedFirstName =
            firstName

        let submittedLastName =
            lastName

        let submittedBirthDate =
            selectedBirthDate

        let submittedGender =
            selectedGender.apiValue


        Task { @MainActor in

            defer {

                isSaving =
                    false
            }


            do {

                let updatedProfile =
                    try await ProfileService()
                        .updateProfile(
                            accessToken:
                                session.accessToken,
                            firstName:
                                submittedFirstName,
                            lastName:
                                submittedLastName,
                            dateOfBirth:
                                submittedBirthDate,
                            gender:
                                submittedGender
                        )


                firstName =
                    updatedProfile.firstName
                    ?? ""


                lastName =
                    updatedProfile.lastName
                    ?? ""


                selectedBirthDate =
                    parseAPIDate(
                        updatedProfile.dateOfBirth
                    )


                selectedGender =
                    Gender.fromAPI(
                        updatedProfile.gender
                    )


                showSuccessAlert =
                    true


            } catch ProfileServiceError.unauthorized {

                sessionManager.invalidateSession(
                    accessToken:
                        session.accessToken
                )


            } catch is CancellationError {

                return


            } catch {

                errorMessage =
                    error.localizedDescription
            }
        }
    }


    // MARK: - Parse Date

    private func parseAPIDate(
        _ value: String?
    ) -> Date? {

        guard
            let value,
            !value.isEmpty

        else {

            return nil
        }


        let formatter =
            DateFormatter()


        formatter.calendar =
            Calendar(
                identifier:
                    .gregorian
            )


        formatter.locale =
            Locale(
                identifier:
                    "en_US_POSIX"
            )


        formatter.timeZone =
            TimeZone(
                secondsFromGMT:
                    0
            )


        formatter.dateFormat =
            "yyyy-MM-dd"


        return formatter.date(
            from:
                value
        )
    }
}
