import SwiftUI

struct ModuleTasksView: View {

    let module: StudyModule
    let session: AuthSession

    @EnvironmentObject private var sessionManager: SessionManager

    @State private var tasks: [StudyTask] = []

    @State private var isLoading = false
    @State private var hasLoaded = false
    @State private var errorMessage: String?


    var body: some View {

        ScrollView {

            LazyVStack(
                spacing: 14
            ) {

                header


                if isLoading
                    && tasks.isEmpty {

                    loadingView
                }


                if let errorMessage {

                    errorCard(
                        errorMessage
                    )
                }


                if hasLoaded
                    && tasks.isEmpty
                    && !isLoading {

                    emptyState
                }


                ForEach(
                    tasks
                ) { task in

                    taskCard(
                        task
                    )
                }
            }
            .frame(
                maxWidth: 600
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
        .navigationTitle(
            "Aufgaben"
        )
        .navigationBarTitleDisplayMode(
            .inline
        )
        .refreshable {

            await loadTasks()
        }
        .task {

            await loadTasks()
        }
    }


    // MARK: - Header

    private var header:
        some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            HStack(
                alignment: .center,
                spacing: 12
            ) {

                RoundedRectangle(
                    cornerRadius: 4,
                    style: .continuous
                )
                .fill(
                    color(
                        for:
                            module.color
                    )
                )
                .frame(
                    width: 7,
                    height: 52
                )


                VStack(
                    alignment: .leading,
                    spacing: 5
                ) {

                    Text(
                        module.name
                    )
                    .font(
                        .system(
                            size: 28,
                            weight: .bold
                        )
                    )


                    if let code =
                        nonEmpty(
                            module.code
                        ) {

                        Text(code)
                            .font(
                                .system(
                                    size: 13,
                                    weight:
                                        .semibold
                                )
                            )
                            .foregroundStyle(
                                color(
                                    for:
                                        module.color
                                )
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
                                color(
                                    for:
                                        module.color
                                )
                                .opacity(
                                    0.10
                                ),
                                in:
                                    Capsule()
                            )
                    }
                }


                Spacer()
            }
        }
    }


    // MARK: - Task Card

    private func taskCard(
        _ task: StudyTask
    ) -> some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            HStack(
                alignment: .top,
                spacing: 12
            ) {

                VStack(
                    alignment: .leading,
                    spacing: 6
                ) {

                    Text(
                        task.title
                    )
                    .font(
                        .system(
                            size: 17,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(
                        .primary
                    )


                    if let description =
                        nonEmpty(
                            task.description
                        ) {

                        Text(
                            description
                        )
                        .font(
                            .system(
                                size: 14
                            )
                        )
                        .foregroundStyle(
                            .secondary
                        )
                        .lineLimit(
                            3
                        )
                    }
                }


                Spacer()


                statusBadge(
                    task
                )
            }


            if let dueDate =
                task.dueDate {

                Label {

                    Text(
                        formattedDueDate(
                            dueDate
                        )
                    )
                } icon: {

                    Image(
                        systemName:
                            "calendar"
                    )
                }
                .font(
                    .system(
                        size: 13,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
                )
            }


            if let source =
                task.externalSource {

                Label(
                    source.courseName,
                    systemImage:
                        "link"
                )
                .font(
                    .system(
                        size: 12,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
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
                    Color.secondary
                        .opacity(
                            0.08
                        ),
                    in:
                        Capsule()
                )
            }
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
        .padding(
            16
        )
        .background(
            Color(
                uiColor:
                    .systemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 18,
                    style:
                        .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
            .stroke(
                Color.secondary
                    .opacity(
                        0.11
                    ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.black
                    .opacity(
                        0.035
                    ),
            radius: 9,
            y: 3
        )
    }


    // MARK: - Status Badge

    private func statusBadge(
        _ task: StudyTask
    ) -> some View {

        Text(
            task.isCompleted
                ? "Erledigt"
                : "Offen"
        )
        .font(
            .system(
                size: 12,
                weight: .semibold
            )
        )
        .foregroundStyle(
            task.isCompleted
                ? Color.green
                : Color.orange
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
            (
                task.isCompleted
                    ? Color.green
                    : Color.orange
            )
            .opacity(
                0.10
            ),
            in:
                Capsule()
        )
    }


    // MARK: - Loading

    private var loadingView:
        some View {

        VStack(
            spacing: 12
        ) {

            ProgressView()


            Text(
                "Aufgaben werden geladen …"
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
            50
        )
    }


    // MARK: - Empty State

    private var emptyState:
        some View {

        VStack(
            spacing: 16
        ) {

            Image(
                systemName:
                    "checklist"
            )
            .font(
                .system(
                    size: 32,
                    weight: .medium
                )
            )
            .foregroundStyle(
                .secondary
            )


            VStack(
                spacing: 6
            ) {

                Text(
                    "Noch keine Aufgaben"
                )
                .font(
                    .headline
                )


                Text(
                    "Für dieses Lernmodul "
                    + "gibt es noch keine Aufgaben."
                )
                .font(
                    .subheadline
                )
                .foregroundStyle(
                    .secondary
                )
                .multilineTextAlignment(
                    .center
                )
            }
        }
        .frame(
            maxWidth: .infinity
        )
        .padding(
            .vertical,
            42
        )
        .padding(
            .horizontal,
            24
        )
        .background(
            Color(
                uiColor:
                    .systemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 18,
                    style:
                        .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
            .stroke(
                Color.secondary
                    .opacity(
                        0.11
                    ),
                lineWidth: 1
            )
        }
    }


    // MARK: - Error

    private func errorCard(
        _ message: String
    ) -> some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

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
            }


            Button {

                Task {

                    await loadTasks()
                }

            } label: {

                Label(
                    "Erneut versuchen",
                    systemImage:
                        "arrow.clockwise"
                )
                .font(
                    .subheadline
                )
                .fontWeight(
                    .semibold
                )
            }
            .disabled(
                isLoading
            )
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
        .padding(
            16
        )
        .background(
            Color.red
                .opacity(
                    0.07
                ),
            in:
                RoundedRectangle(
                    cornerRadius: 14,
                    style:
                        .continuous
                )
        )
    }


    // MARK: - Aufgaben laden

    @MainActor
    private func loadTasks()
        async {

        guard !isLoading
        else {
            return
        }


        guard sessionManager
            .session?
            .accessToken
            == session.accessToken
        else {
            return
        }


        sessionManager
            .checkExpiration()


        guard sessionManager
            .session?
            .accessToken
            == session.accessToken
        else {
            return
        }


        isLoading = true

        errorMessage = nil


        defer {

            isLoading = false
        }


        do {

            let result =
                try await StudyTaskService()
                    .getByModule(
                        moduleId:
                            module.id,
                        accessToken:
                            session.accessToken
                    )


            try Task
                .checkCancellation()


            sessionManager
                .checkExpiration()


            guard sessionManager
                .session?
                .accessToken
                == session.accessToken
            else {
                return
            }


            tasks =
                result


            hasLoaded =
                true


        } catch is CancellationError {

            return


        } catch {

            guard !Task.isCancelled,

                  sessionManager
                    .session?
                    .accessToken
                    == session.accessToken

            else {
                return
            }


            if let serviceError =
                error as?
                    StudyTaskServiceError,

               case .unauthorized =
                    serviceError {

                tasks = []


                sessionManager
                    .invalidateSession(
                        accessToken:
                            session.accessToken
                    )


                return
            }


            errorMessage =
                error.localizedDescription
        }
    }


    // MARK: - Helpers

    private func color(
        for hex: String?
    ) -> Color {

        guard let hex,

              let parsed =
                Color(
                    hex: hex
                )

        else {

            return
                .accentColor
        }


        return parsed
    }


    private func nonEmpty(
        _ value: String?
    ) -> String? {

        guard let value,

              !value
                .trimmingCharacters(
                    in:
                        .whitespacesAndNewlines
                )
                .isEmpty

        else {

            return nil
        }


        return value
    }
    
    private func formattedDueDate(
        _ date: Date
    ) -> String {

        let formatter = DateFormatter()

        formatter.locale = Locale(
            identifier: "de_DE"
        )

        formatter.dateFormat =
            "dd.MM.yyyy, HH:mm 'Uhr'"

        return formatter.string(
            from: date
        )
    }
}


// MARK: - Hex Farbe

private extension Color {

    init?(
        hex: String
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


        guard hex.count
                == 6,

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
