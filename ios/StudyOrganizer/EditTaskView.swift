import SwiftUI

struct EditTaskView: View {

    let module: StudyModule
    let task: StudyTask
    let session: AuthSession
    let onUpdated: (StudyTask) -> Void


    @EnvironmentObject
    private var sessionManager: SessionManager


    @Environment(\.dismiss)
    private var dismiss


    @State
    private var title: String

    @State
    private var description: String

    @State
    private var dueDate: Date


    @State
    private var submitted = false

    @State
    private var isSubmitting = false

    @State
    private var requestError: String?


    @FocusState
    private var focusedField: Field?


    private enum Field: Hashable {

        case title
        case description
    }


    // MARK: - Initialisierung

    init(
        module: StudyModule,
        task: StudyTask,
        session: AuthSession,
        onUpdated: @escaping (StudyTask) -> Void
    ) {

        self.module = module
        self.task = task
        self.session = session
        self.onUpdated = onUpdated


        _title =
            State(
                initialValue:
                    task.title
            )


        _description =
            State(
                initialValue:
                    task.description ?? ""
            )


        _dueDate =
            State(
                initialValue:
                    task.dueDate ?? Date()
            )
    }


    // MARK: - Normalisierte Werte

    private var normalizedTitle: String {

        title.trimmingCharacters(
            in: .whitespacesAndNewlines
        )
    }


    private var normalizedDescription: String {

        description.trimmingCharacters(
            in: .whitespacesAndNewlines
        )
    }


    // MARK: - Validierung

    private var titleError: String? {

        if normalizedTitle.isEmpty {

            return
                "Bitte gib einen Titel für die Aufgabe ein."
        }

        return nil
    }


    // MARK: - View

    var body: some View {

        NavigationStack {

            ScrollView {

                VStack(
                    spacing: 20
                ) {

                    header

                    formCard


                    if let requestError {

                        serverErrorCard(
                            requestError
                        )
                    }


                    saveButton
                }
                .frame(
                    maxWidth: 520
                )
                .frame(
                    maxWidth: .infinity
                )
                .padding(
                    .horizontal,
                    18
                )
                .padding(
                    .top,
                    18
                )
                .padding(
                    .bottom,
                    40
                )
            }
            .background(
                Color(
                    uiColor:
                        .systemGroupedBackground
                )
                .ignoresSafeArea()
            )
            .contentShape(
                Rectangle()
            )
            .onTapGesture {

                focusedField = nil
            }
            .scrollDismissesKeyboard(
                .interactively
            )
            .toolbar {

                ToolbarItem(
                    placement:
                        .cancellationAction
                ) {

                    Button(
                        "Abbrechen"
                    ) {

                        dismiss()
                    }
                    .disabled(
                        isSubmitting
                    )
                }
            }
            .interactiveDismissDisabled(
                isSubmitting
            )
        }
        .onChange(
            of:
                sessionManager
                    .session?
                    .accessToken
        ) {

            if sessionManager
                .session?
                .accessToken
                != session.accessToken {

                dismiss()
            }
        }
    }


    // MARK: - Header

    private var header:
        some View {

        VStack(
            spacing: 10
        ) {

            Image(
                systemName:
                    "square.and.pencil"
            )
            .font(
                .system(
                    size: 27,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                .tint
            )
            .frame(
                width: 60,
                height: 60
            )
            .background(
                Color.accentColor
                    .opacity(
                        0.11
                    ),
                in:
                    RoundedRectangle(
                        cornerRadius: 17,
                        style: .continuous
                    )
            )
            .accessibilityHidden(
                true
            )


            Text(
                "Aufgabe bearbeiten"
            )
            .font(
                .title2
            )
            .fontWeight(
                .bold
            )


            Text(
                "Passe deine Aufgabe an."
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
    }


    // MARK: - Formular-Card

    private var formCard:
        some View {

        VStack(
            alignment: .leading,
            spacing: 18
        ) {

            // Titel

            VStack(
                alignment: .leading,
                spacing: 8
            ) {

                HStack(
                    spacing: 3
                ) {

                    Text(
                        "Titel"
                    )
                    .font(
                        .system(
                            size: 17,
                            weight: .semibold
                        )
                    )


                    Text(
                        "*"
                    )
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(
                        .red
                    )
                }


                titleField
                    .submitLabel(
                        .next
                    )
                    .onSubmit {

                        focusedField =
                            .description
                    }


                if submitted,
                   let titleError {

                    errorLabel(
                        titleError
                    )
                }
            }


            // Beschreibung

            VStack(
                alignment: .leading,
                spacing: 8
            ) {

                Text(
                    "Beschreibung"
                )
                .font(
                    .system(
                        size: 17,
                        weight: .semibold
                    )
                )


                descriptionField
            }


            // Fälligkeitsdatum

            VStack(
                alignment: .leading,
                spacing: 8
            ) {

                HStack(
                    spacing: 3
                ) {

                    Text(
                        "Fälligkeitsdatum"
                    )
                    .font(
                        .system(
                            size: 17,
                            weight: .semibold
                        )
                    )


                    Text(
                        "*"
                    )
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(
                        .red
                    )
                }


                dueDateField
            }


            // Lernmodul

            VStack(
                alignment: .leading,
                spacing: 8
            ) {

                Text(
                    "Lernmodul"
                )
                .font(
                    .system(
                        size: 17,
                        weight: .semibold
                    )
                )


                moduleField
            }
        }
        .padding(
            18
        )
        .background(
            Color(
                uiColor:
                    .systemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 22,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 22,
                style: .continuous
            )
            .stroke(
                Color.secondary
                    .opacity(
                        0.12
                    ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.black
                    .opacity(
                        0.04
                    ),
            radius: 12,
            y: 4
        )
    }


    // MARK: - Titel-Feld

    private var titleField:
        some View {

        HStack(
            spacing: 11
        ) {

            Image(
                systemName:
                    "checklist"
            )
            .font(
                .system(
                    size: 16,
                    weight: .medium
                )
            )
            .foregroundStyle(
                focusedField == .title
                    ? Color.accentColor
                    : Color.secondary
            )
            .frame(
                width: 20
            )
            .accessibilityHidden(
                true
            )


            TextField(
                "z. B. Übungsblatt 4",
                text: $title
            )
            .focused(
                $focusedField,
                equals: .title
            )
        }
        .padding(
            .horizontal,
            13
        )
        .frame(
            minHeight: 46
        )
        .background(
            Color(
                uiColor:
                    .secondarySystemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 12,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
            .stroke(
                borderColor(
                    for: .title
                ),
                lineWidth:
                    focusedField == .title
                    ? 1.7
                    : 1
            )
        }
        .animation(
            .easeOut(
                duration: 0.16
            ),
            value: focusedField
        )
    }


    // MARK: - Beschreibung

    private var descriptionField:
        some View {

        HStack(
            alignment: .top,
            spacing: 11
        ) {

            Image(
                systemName:
                    "text.alignleft"
            )
            .font(
                .system(
                    size: 16,
                    weight: .medium
                )
            )
            .foregroundStyle(
                focusedField == .description
                    ? Color.accentColor
                    : Color.secondary
            )
            .frame(
                width: 20
            )
            .padding(
                .top,
                2
            )
            .accessibilityHidden(
                true
            )


            TextField(
                "Beschreibung optional",
                text: $description,
                axis: .vertical
            )
            .lineLimit(
                3...5
            )
            .focused(
                $focusedField,
                equals: .description
            )
        }
        .padding(
            13
        )
        .frame(
            minHeight: 90,
            alignment: .top
        )
        .background(
            Color(
                uiColor:
                    .secondarySystemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 12,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
            .stroke(
                borderColor(
                    for: .description
                ),
                lineWidth:
                    focusedField == .description
                    ? 1.7
                    : 1
            )
        }
        .animation(
            .easeOut(
                duration: 0.16
            ),
            value: focusedField
        )
    }


    // MARK: - Fälligkeitsdatum

    private var dueDateField:
        some View {

        VStack(
            spacing: 0
        ) {

            HStack(
                spacing: 11
            ) {

                Image(
                    systemName:
                        "calendar"
                )
                .font(
                    .system(
                        size: 16,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
                )
                .frame(
                    width: 20
                )
                .accessibilityHidden(
                    true
                )


                Text(
                    "Datum"
                )
                .font(
                    .system(
                        size: 15,
                        weight: .medium
                    )
                )


                Spacer()


                DatePicker(
                    "",
                    selection: $dueDate,
                    displayedComponents:
                        .date
                )
                .labelsHidden()
                .datePickerStyle(
                    .compact
                )
                .scaleEffect(
                    0.92
                )
            }
            .frame(
                minHeight: 46
            )


            Divider()


            HStack(
                spacing: 11
            ) {

                Image(
                    systemName:
                        "clock"
                )
                .font(
                    .system(
                        size: 16,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
                )
                .frame(
                    width: 20
                )
                .accessibilityHidden(
                    true
                )


                Text(
                    "Uhrzeit"
                )
                .font(
                    .system(
                        size: 15,
                        weight: .medium
                    )
                )


                Spacer()


                DatePicker(
                    "",
                    selection: $dueDate,
                    displayedComponents:
                        .hourAndMinute
                )
                .labelsHidden()
                .datePickerStyle(
                    .compact
                )
                .scaleEffect(
                    0.92
                )
            }
            .frame(
                minHeight: 40
            )
        }
        .padding(
            .horizontal,
            13
        )
        .padding(
            .vertical,
            2
        )
        .background(
            Color(
                uiColor:
                    .secondarySystemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 12,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
            .stroke(
                Color.secondary
                    .opacity(
                        0.22
                    ),
                lineWidth: 1
            )
        }
    }


    // MARK: - Lernmodul

    private var moduleField:
        some View {

        HStack(
            spacing: 11
        ) {

            Circle()
                .fill(
                    moduleColor
                )
                .frame(
                    width: 10,
                    height: 10
                )


            Text(
                module.name
            )
            .fontWeight(
                .medium
            )


            if let code =
                nonEmpty(
                    module.code
                ) {

                Text(
                    code
                )
                .font(
                    .system(
                        size: 13,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    moduleColor
                )
                .padding(
                    .horizontal,
                    8
                )
                .padding(
                    .vertical,
                    4
                )
                .background(
                    moduleColor
                        .opacity(
                            0.10
                        ),
                    in:
                        Capsule()
                )
            }


            Spacer()
        }
        .padding(
            .horizontal,
            13
        )
        .frame(
            minHeight: 46
        )
        .background(
            Color(
                uiColor:
                    .secondarySystemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 12,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
            .stroke(
                Color.secondary
                    .opacity(
                        0.22
                    ),
                lineWidth: 1
            )
        }
    }


    // MARK: - Speichern-Button

    private var saveButton:
        some View {

        Button {

            focusedField = nil

            save()

        } label: {

            HStack(
                spacing: 8
            ) {

                if isSubmitting {

                    ProgressView()
                        .tint(
                            .white
                        )
                }


                Text(
                    isSubmitting
                    ? "Wird gespeichert …"
                    : "Änderungen speichern"
                )
                .fontWeight(
                    .semibold
                )
            }
            .foregroundStyle(
                .white
            )
            .frame(
                maxWidth: .infinity
            )
            .frame(
                height: 52
            )
            .background(
                Color.accentColor,
                in:
                    RoundedRectangle(
                        cornerRadius: 14,
                        style: .continuous
                    )
            )
        }
        .buttonStyle(
            .plain
        )
        .disabled(
            isSubmitting
        )
        .opacity(
            isSubmitting
            ? 0.75
            : 1
        )
    }


    // MARK: - Serverfehler

    private func serverErrorCard(
        _ message: String
    ) -> some View {

        HStack(
            alignment: .top,
            spacing: 10
        ) {

            Image(
                systemName:
                    "exclamationmark.circle.fill"
            )
            .foregroundStyle(
                .red
            )


            Text(
                message
            )
            .font(
                .subheadline
            )
            .foregroundStyle(
                .red
            )


            Spacer(
                minLength: 0
            )
        }
        .padding(
            14
        )
        .frame(
            maxWidth: .infinity
        )
        .background(
            Color.red
                .opacity(
                    0.07
                ),
            in:
                RoundedRectangle(
                    cornerRadius: 12,
                    style: .continuous
                )
        )
    }


    // MARK: - Rahmenfarbe

    private func borderColor(
        for field: Field
    ) -> Color {

        if submitted {

            switch field {

            case .title:

                if titleError != nil {

                    return .red
                }


            case .description:

                break
            }
        }


        if focusedField == field {

            return .accentColor
        }


        return
            Color.secondary
                .opacity(
                    0.22
                )
    }


    // MARK: - Fehlermeldung

    private func errorLabel(
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
        .fixedSize(
            horizontal: false,
            vertical: true
        )
    }


    // MARK: - Speichern

    @MainActor
    private func save() {

        guard !isSubmitting
        else {

            return
        }


        requestError =
            nil


        submitted =
            true


        guard titleError == nil
        else {

            focusedField =
                .title

            return
        }


        sessionManager
            .checkExpiration()


        guard
            sessionManager
                .session?
                .accessToken
                == session.accessToken

        else {

            dismiss()

            return
        }


        let submittedTitle =
            normalizedTitle


        let submittedDescription =
            normalizedDescription.isEmpty
            ? nil
            : normalizedDescription


        let submittedDueDate =
            dueDate


        focusedField =
            nil


        isSubmitting =
            true


        Task { @MainActor in

            defer {

                isSubmitting =
                    false
            }


            do {

                let updatedTask =
                    try await StudyTaskService()
                        .update(
                            moduleId:
                                module.id,
                            taskId:
                                task.id,
                            title:
                                submittedTitle,
                            description:
                                submittedDescription,
                            dueDate:
                                submittedDueDate,
                            accessToken:
                                session.accessToken
                        )


                try Task
                    .checkCancellation()


                sessionManager
                    .checkExpiration()


                guard
                    sessionManager
                        .session?
                        .accessToken
                        == session.accessToken

                else {

                    return
                }


                onUpdated(
                    updatedTask
                )


                dismiss()


            } catch is CancellationError {

                return


            } catch {

                sessionManager
                    .checkExpiration()


                guard
                    sessionManager
                        .session?
                        .accessToken
                        == session.accessToken

                else {

                    return
                }


                if let serviceError =
                    error
                        as? StudyTaskServiceError,

                   case .unauthorized =
                    serviceError {

                    sessionManager
                        .invalidateSession(
                            accessToken:
                                session.accessToken
                        )


                    dismiss()

                    return
                }


                requestError =
                    error.localizedDescription
            }
        }
    }


    // MARK: - Helper

    private func nonEmpty(
        _ value: String?
    ) -> String? {

        guard let value
        else {

            return nil
        }


        let normalized =
            value.trimmingCharacters(
                in:
                    .whitespacesAndNewlines
            )


        return
            normalized.isEmpty
            ? nil
            : normalized
    }


    private var moduleColor:
        Color {

        guard
            let hex = module.color,

            let parsed =
                Color(
                    editTaskHex: hex
                )

        else {

            return .accentColor
        }


        return parsed
    }
}


// MARK: - Hex Farbe

private extension Color {

    init?(
        editTaskHex hex: String
    ) {

        var hex =
            hex.trimmingCharacters(
                in:
                    .whitespacesAndNewlines
            )


        if hex.hasPrefix(
            "#"
        ) {

            hex.removeFirst()
        }


        guard
            hex.count == 6,

            let value =
                UInt64(
                    hex,
                    radix: 16
                )

        else {

            return nil
        }


        let red =
            Double(
                (value >> 16)
                & 0xFF
            )
            / 255


        let green =
            Double(
                (value >> 8)
                & 0xFF
            )
            / 255


        let blue =
            Double(
                value
                & 0xFF
            )
            / 255


        self.init(
            red: red,
            green: green,
            blue: blue
        )
    }
}
