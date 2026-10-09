import SwiftUI


struct TaskCardView: View {

    enum Style {

        case compact
        case standard
        case management
    }


    let task: StudyTask
    let module: StudyModule
    let style: Style


    var onToggleStatus: (() -> Void)? = nil
    var onEdit: (() -> Void)? = nil
    var onDelete: (() -> Void)? = nil


    // MARK: - Status

    private var isOverdue: Bool {

        guard
            !task.isCompleted,
            let dueDate = task.dueDate

        else {

            return false
        }


        return dueDate < Date()
    }


    private var statusColor: Color {

        if task.isCompleted {

            return .green
        }


        if isOverdue {

            return .red
        }


        return .orange
    }


    private var statusTitle: String {

        if task.isCompleted {

            return "Erledigt"
        }


        if isOverdue {

            return "Überfällig"
        }


        return "Offen"
    }


    private var moduleAccent: Color {

        guard
            let color = module.color,
            let parsedColor = Color(
                taskCardHex: color
            )

        else {

            return .accentColor
        }


        return parsedColor
    }


    private var showsModuleInformation: Bool {

        switch style {

        case .compact,
             .standard:

            return true


        case .management:

            return false
        }
    }


    private var showsActions: Bool {

        style == .management
    }


    // MARK: - Body

    var body: some View {

        HStack(
            spacing: 11
        ) {

            statusBar


            VStack(
                alignment: .leading,
                spacing: contentSpacing
            ) {

                topRow


                taskTitle


                taskDescription


                if showsModuleInformation {

                    moduleName
                }


                dueDateRow


                externalSourceView


                if showsActions {

                    managementActions
                }
            }
            .padding(
                .vertical,
                verticalPadding
            )
            .padding(
                .trailing,
                13
            )
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
        .background(
            cardBackground
        )
        .clipShape(
            RoundedRectangle(
                cornerRadius: 17,
                style: .continuous
            )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 17,
                style: .continuous
            )
            .stroke(
                Color.secondary.opacity(
                    0.075
                ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.black.opacity(
                    0.025
                ),
            radius: 6,
            x: 0,
            y: 2
        )
    }


    // MARK: - Status Bar

    private var statusBar: some View {

        RoundedRectangle(
            cornerRadius: 3,
            style: .continuous
        )
        .fill(
            statusColor
        )
        .frame(
            width: 3
        )
        .padding(
            .vertical,
            7
        )
    }


    // MARK: - Top Row

    private var topRow: some View {

        HStack(
            alignment: .center
        ) {

            if showsModuleInformation {

                moduleBadge
            }


            Spacer()


            statusBadge
        }
    }


    // MARK: - Module Badge

    private var moduleBadge: some View {

        Text(
            moduleCode
        )
        .font(
            .system(
                size: 11,
                weight: .bold
            )
        )
        .foregroundStyle(
            moduleAccent
        )
        .padding(
            .horizontal,
            9
        )
        .padding(
            .vertical,
            5
        )
        .background(
            moduleAccent.opacity(
                0.20
            ),
            in:
                Capsule()
        )
    }


    // MARK: - Status Badge

    private var statusBadge: some View {

        Text(
            statusTitle
        )
        .font(
            .system(
                size: 10,
                weight: .semibold
            )
        )
        .foregroundStyle(
            statusColor
        )
        .padding(
            .horizontal,
            7
        )
        .padding(
            .vertical,
            3
        )
        .background(
            statusColor.opacity(
                0.07
            ),
            in:
                Capsule()
        )
    }


    // MARK: - Titel

    private var taskTitle: some View {

        Text(
            task.title
        )
        .font(
            .system(
                size: titleSize,
                weight: .semibold
            )
        )
        .foregroundStyle(
            task.isCompleted
                ? Color.secondary
                : Color.primary
        )
        .strikethrough(
            task.isCompleted,
            color:
                .secondary
        )
        .lineLimit(
            style == .compact
                ? 2
                : 3
        )
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
    }


    // MARK: - Modulname

    private var moduleName: some View {

        Text(
            module.name
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
        .lineLimit(
            1
        )
    }
    
    // MARK: - Beschreibung

    @ViewBuilder
    private var taskDescription: some View {

        if style == .management,
           let description =
            task.description?
                .trimmingCharacters(
                    in:
                        .whitespacesAndNewlines
                ),
           !description.isEmpty {

            Text(
                description
            )
            .font(
                .system(
                    size: 13,
                    weight: .regular
                )
            )
            .foregroundStyle(
                .secondary
            )
            .lineLimit(
                3
            )
            .frame(
                maxWidth: .infinity,
                alignment: .leading
            )
        }
    }


    // MARK: - Externe Quelle

    @ViewBuilder
    private var externalSourceView: some View {

        if style == .management,
           let source =
            task.externalSource {

            Label(
                source.courseName,
                systemImage:
                    "link"
            )
            .font(
                .system(
                    size: 11.5,
                    weight: .medium
                )
            )
            .foregroundStyle(
                .secondary
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
                Color.secondary.opacity(
                    0.07
                ),
                in:
                    Capsule()
            )
        }
    }


    // MARK: - Datum

    @ViewBuilder
    private var dueDateRow: some View {

        if let dueDate = task.dueDate {

            HStack(
                spacing: 5
            ) {

                Image(
                    systemName:
                        "calendar"
                )
                .foregroundStyle(
                    isOverdue
                        ? Color.red.opacity(
                            0.78
                        )
                        : Color.secondary
                )


                Text(
                    formattedDate(
                        dueDate
                    )
                )
                .foregroundStyle(
                    .secondary
                )
            }
            .font(
                .system(
                    size: 11.5,
                    weight: .medium
                )
            )
        }
    }


    // MARK: - Management Actions

    @ViewBuilder
    private var managementActions: some View {

        if onToggleStatus != nil
            || onEdit != nil
            || onDelete != nil {

            Divider()
                .padding(
                    .top,
                    3
                )


            HStack(
                spacing: 10
            ) {

                // Status ändern

                if let onToggleStatus {

                    Button {

                        onToggleStatus()

                    } label: {

                        Label(
                            task.isCompleted
                                ? "Wieder öffnen"
                                : "Erledigen",
                            systemImage:
                                task.isCompleted
                                    ? "arrow.uturn.backward"
                                    : "checkmark"
                        )
                        .font(
                            .system(
                                size: 13,
                                weight: .bold
                            )
                        )
                        .foregroundStyle(
                            task.isCompleted
                                ? Color.orange
                                : Color.green
                        )
                        .padding(
                            .horizontal,
                            13
                        )
                        .frame(
                            height: 38
                        )
                        .background(
                            (
                                task.isCompleted
                                    ? Color.orange
                                    : Color.green
                            )
                            .opacity(
                                0.18
                            ),
                            in:
                                Capsule()
                        )
                    }
                    .buttonStyle(
                        .plain
                    )
                }


                Spacer(
                    minLength: 4
                )


                // Bearbeiten

                if let onEdit {

                    Button {

                        onEdit()

                    } label: {

                        Label(
                            "Bearbeiten",
                            systemImage:
                                "pencil"
                        )
                        .font(
                            .system(
                                size: 13,
                                weight: .bold
                            )
                        )
                        .foregroundStyle(
                            Color.accentColor
                        )
                        .padding(
                            .horizontal,
                            13
                        )
                        .frame(
                            height: 38
                        )
                        .background(
                            Color.accentColor.opacity(
                                0.12
                            ),
                            in:
                                Capsule()
                        )
                    }
                    .buttonStyle(
                        .plain
                    )
                }


                // Löschen

                if let onDelete {

                    Button(
                        role:
                            .destructive
                    ) {

                        onDelete()

                    } label: {

                        Image(
                            systemName:
                                "trash"
                        )
                        .font(
                            .system(
                                size: 14,
                                weight: .semibold
                            )
                        )
                        .foregroundStyle(
                            .red
                        )
                        .frame(
                            width: 38,
                            height: 38
                        )
                        .background(
                            Color.red.opacity(
                                0.09
                            ),
                            in:
                                Circle()
                        )
                    }
                    .buttonStyle(
                        .plain
                    )
                }
            }
            .padding(
                .top,
                2
            )
        }
    }


    // MARK: - Background

    private var cardBackground: some View {

        ZStack {

            RoundedRectangle(
                cornerRadius: 17,
                style: .continuous
            )
            .fill(
                Color(
                    uiColor:
                        .systemBackground
                )
            )


            RoundedRectangle(
                cornerRadius: 17,
                style: .continuous
            )
            .fill(
                LinearGradient(
                    colors: [
                        statusColor.opacity(
                            backgroundTintOpacity
                        ),
                        Color.clear
                    ],
                    startPoint:
                        .topLeading,
                    endPoint:
                        .bottomTrailing
                )
            )
        }
    }


    // MARK: - Style Werte

    private var contentSpacing: CGFloat {

        switch style {

        case .compact:

            return 6


        case .standard:

            return 6


        case .management:

            return 8
        }
    }


    private var verticalPadding: CGFloat {

        switch style {

        case .compact:

            return 11


        case .standard:

            return 11


        case .management:

            return 13
        }
    }


    private var titleSize: CGFloat {

        switch style {

        case .compact:

            return 15.5


        case .standard:

            return 15.5


        case .management:

            return 16
        }
    }


    private var backgroundTintOpacity: Double {

        if isOverdue {

            return 0.018
        }


        if task.isCompleted {

            return 0.012
        }


        return 0.008
    }


    // MARK: - Helpers

    private var moduleCode: String {

        if let code =
            module.code?
                .trimmingCharacters(
                    in:
                        .whitespacesAndNewlines
                ),
           !code.isEmpty {

            return code
        }


        return "MODUL"
    }


    private func formattedDate(
        _ date: Date
    ) -> String {

        let formatter =
            DateFormatter()


        formatter.locale =
            Locale(
                identifier:
                    "de_DE"
            )


        formatter.dateFormat =
            "dd.MM.yyyy, HH:mm 'Uhr'"


        return formatter.string(
            from:
                date
        )
    }
}


// MARK: - Hex Color

private extension Color {

    init?(
        taskCardHex hex: String
    ) {

        var value =
            hex.trimmingCharacters(
                in:
                    .whitespacesAndNewlines
            )


        if value.hasPrefix(
            "#"
        ) {

            value.removeFirst()
        }


        guard
            value.count == 6,
            let number =
                UInt64(
                    value,
                    radix:
                        16
                )

        else {

            return nil
        }


        let red =
            Double(
                (
                    number >> 16
                )
                & 0xFF
            )
            / 255


        let green =
            Double(
                (
                    number >> 8
                )
                & 0xFF
            )
            / 255


        let blue =
            Double(
                number
                & 0xFF
            )
            / 255


        self.init(
            red:
                red,
            green:
                green,
            blue:
                blue
        )
    }
}
