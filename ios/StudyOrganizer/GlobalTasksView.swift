import SwiftUI


struct GlobalTasksView: View {

    let session: AuthSession
    let refreshID: Int


    @EnvironmentObject
    private var sessionManager: SessionManager


    @State
    private var modules: [StudyModule] = []


    @State
    private var globalTasks: [GlobalTaskItem] = []


    @State
    private var selectedFilter: TaskFilter = .all


    @State
    private var isLoading = false


    @State
    private var hasLoaded = false


    @State
    private var errorMessage: String?


    @State
    private var animateContent = false


    // MARK: - Global Task

    private struct GlobalTaskItem: Identifiable {

        let task: StudyTask
        let module: StudyModule


        var id: String {
            task.id
        }
    }


    // MARK: - Filter

    private enum TaskFilter: String, CaseIterable {

        case all = "Alle"
        case open = "Offen"
        case overdue = "Überfällig"
        case completed = "Erledigt"
    }


    // MARK: - Sortierung

    private var sortedTasks: [GlobalTaskItem] {

        globalTasks.sorted { lhs, rhs in

            let lhsGroup =
                sortGroup(
                    lhs.task
                )

            let rhsGroup =
                sortGroup(
                    rhs.task
                )


            if lhsGroup != rhsGroup {

                return lhsGroup < rhsGroup
            }


            switch (
                lhs.task.dueDate,
                rhs.task.dueDate
            ) {

            case let (
                lhsDate?,
                rhsDate?
            ):

                if lhsDate != rhsDate {

                    return lhsDate < rhsDate
                }


            case (
                .some,
                .none
            ):

                return true


            case (
                .none,
                .some
            ):

                return false


            case (
                .none,
                .none
            ):

                break
            }


            return lhs.task.title
                .localizedCaseInsensitiveCompare(
                    rhs.task.title
                ) == .orderedAscending
        }
    }


    // MARK: - Sichtbare Aufgaben

    private var visibleTasks: [GlobalTaskItem] {

        sortedTasks.filter { item in

            switch selectedFilter {

            case .all:

                return true


            case .open:

                return
                    !item.task.isCompleted
                    && !isOverdue(
                        item.task
                    )


            case .overdue:

                return isOverdue(
                    item.task
                )


            case .completed:

                return item.task.isCompleted
            }
        }
    }


    // MARK: - Body

    var body: some View {

        ScrollView {

            LazyVStack(
                alignment: .leading,
                spacing: 18
            ) {

                header


                if isLoading
                    && !hasLoaded {

                    loadingView


                } else if let errorMessage {

                    errorView(
                        errorMessage
                    )


                } else {

                    filterSection


                    tasksSection
                }
            }
            .frame(
                maxWidth: 620
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
                8
            )
            .padding(
                .bottom,
                155
            )
        }
        .background(
            background
        )
        .navigationTitle(
            "Aufgaben"
        )
        .navigationBarTitleDisplayMode(
            .large
        )
        .refreshable {

            await loadTasks()
        }
        .task(
            id: refreshID
        ) {

            await loadTasks()
        }
    }


    // MARK: - Background

    private var background: some View {

        ZStack {

            Color(
                uiColor:
                    .systemGroupedBackground
            )


            LinearGradient(
                colors: [
                    Color.accentColor.opacity(
                        0.025
                    ),
                    Color.clear,
                    Color.clear
                ],
                startPoint:
                    .topLeading,
                endPoint:
                    .center
            )
        }
        .ignoresSafeArea()
    }


    // MARK: - Header

    private var header: some View {

        VStack(
            alignment: .leading,
            spacing: 5
        ) {

            Text(
                "DEINE AUFGABEN"
            )
            .font(
                .system(
                    size: 11.5,
                    weight: .bold
                )
            )
            .tracking(
                1.5
            )
            .foregroundStyle(
                Color.accentColor
            )


            Text(
                "Alle Aufgaben aus deinen "
                + "Lernmodulen an einem Ort."
            )
            .font(
                .system(
                    size: 15.5,
                    weight: .regular
                )
            )
            .foregroundStyle(
                .secondary
            )
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
    }


    // MARK: - Filter Section

    private var filterSection: some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            Text(
                "FILTER"
            )
            .font(
                .system(
                    size: 10.5,
                    weight: .bold
                )
            )
            .tracking(
                1.35
            )
            .foregroundStyle(
                Color.accentColor
            )


            HStack(
                spacing: 7
            ) {

                ForEach(
                    TaskFilter.allCases,
                    id: \.self
                ) { filter in

                    filterButton(
                        filter
                    )
                }
            }
            .frame(
                maxWidth: .infinity,
                alignment: .leading
            )
        }
    }


    // MARK: - Filter Button

    private func filterButton(
        _ filter: TaskFilter
    ) -> some View {

        let isSelected =
            selectedFilter == filter


        return Button {

            withAnimation(
                .spring(
                    response: 0.25,
                    dampingFraction: 0.88
                )
            ) {

                selectedFilter =
                    filter
            }

        } label: {

            HStack(
                spacing: 5
            ) {

                Text(
                    filter.rawValue
                )
                .font(
                    .system(
                        size: 12.5,
                        weight:
                            isSelected
                                ? .semibold
                                : .medium
                    )
                )


                Text(
                    "\(count(for: filter))"
                )
                .font(
                    .system(
                        size: 10.5,
                        weight: .bold
                    )
                )
                .foregroundStyle(
                    isSelected
                        ? Color.white
                        : Color.secondary
                )
                .padding(
                    .horizontal,
                    5
                )
                .padding(
                    .vertical,
                    2
                )
                .background(
                    isSelected
                        ? Color.white.opacity(
                            0.18
                        )
                        : Color.secondary.opacity(
                            0.07
                        ),
                    in:
                        Capsule()
                )
            }
            .foregroundStyle(
                isSelected
                    ? Color.white
                    : Color.secondary
            )
            .padding(
                .horizontal,
                10
            )
            .padding(
                .vertical,
                8
            )
            .background(
                isSelected
                    ? Color.accentColor
                    : Color(
                        uiColor:
                            .secondarySystemGroupedBackground
                    ),
                in:
                    Capsule()
            )
            .overlay {

                if !isSelected {

                    Capsule()
                        .stroke(
                            Color.secondary.opacity(
                                0.06
                            ),
                            lineWidth: 1
                        )
                }
            }
        }
        .buttonStyle(
            .plain
        )
    }


    // MARK: - Tasks Section

    private var tasksSection: some View {

        VStack(
            alignment: .leading,
            spacing: 11
        ) {

            HStack(
                alignment: .center
            ) {

                Text(
                    sectionTitle
                )
                .font(
                    .system(
                        size: 22,
                        weight: .bold
                    )
                )


                Spacer()


                Text(
                    taskCountText
                )
                .font(
                    .system(
                        size: 11.5,
                        weight: .semibold
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
                    Color(
                        uiColor:
                            .secondarySystemGroupedBackground
                    ),
                    in:
                        Capsule()
                )
            }


            if globalTasks.isEmpty {

                emptyState(
                    icon:
                        "checkmark.square",
                    title:
                        "Noch keine Aufgaben",
                    message:
                        "Erstelle in einem Lernmodul "
                        + "deine erste Aufgabe."
                )


            } else if visibleTasks.isEmpty {

                filterEmptyState


            } else {

                LazyVStack(
                    spacing: 9
                ) {

                    ForEach(
                        Array(
                            visibleTasks.enumerated()
                        ),
                        id:
                            \.element.id
                    ) { index, item in

                        TaskCardView(
                            task: item.task,
                            module: item.module,
                            style: .standard
                        )
                        .opacity(
                            animateContent
                                ? 1
                                : 0
                        )
                        .offset(
                            y:
                                animateContent
                                    ? 0
                                    : 7
                        )
                        .animation(
                            .spring(
                                response: 0.38,
                                dampingFraction: 0.89
                            )
                            .delay(
                                Double(index)
                                    * 0.02
                            ),
                            value:
                                animateContent
                        )
                    }
                }
            }
        }
    }


    // MARK: - Section Title

    private var sectionTitle: String {

        switch selectedFilter {

        case .all:

            return "Alle Aufgaben"


        case .open:

            return "Offene Aufgaben"


        case .overdue:

            return "Überfällige Aufgaben"


        case .completed:

            return "Erledigte Aufgaben"
        }
    }


    private var taskCountText: String {

        if visibleTasks.count == 1 {

            return "1 Aufgabe"
        }


        return "\(visibleTasks.count) Aufgaben"
    }


    // MARK: - Filter Empty State

    @ViewBuilder
    private var filterEmptyState: some View {

        switch selectedFilter {

        case .all:

            emptyState(
                icon:
                    "checkmark.square",
                title:
                    "Keine Aufgaben",
                message:
                    "Es sind noch keine "
                    + "Aufgaben vorhanden."
            )


        case .open:

            emptyState(
                icon:
                    "circle",
                title:
                    "Keine offenen Aufgaben",
                message:
                    "Aktuell stehen keine "
                    + "zukünftigen Aufgaben an."
            )


        case .overdue:

            emptyState(
                icon:
                    "checkmark.circle",
                title:
                    "Nichts überfällig",
                message:
                    "Aktuell ist keine "
                    + "Aufgabe überfällig."
            )


        case .completed:

            emptyState(
                icon:
                    "checkmark.circle",
                title:
                    "Noch nichts erledigt",
                message:
                    "Erledigte Aufgaben "
                    + "erscheinen hier."
            )
        }
    }


    // MARK: - Status Badge

    @ViewBuilder
    private func statusBadge(
        _ task: StudyTask
    ) -> some View {

        if task.isCompleted {

            Text(
                "Erledigt"
            )
            .font(
                .system(
                    size: 10,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                Color.green
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
                Color.green.opacity(
                    0.07
                ),
                in:
                    Capsule()
            )


        } else if isOverdue(
            task
        ) {

            Text(
                "Überfällig"
            )
            .font(
                .system(
                    size: 10,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                Color.red
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
                Color.red.opacity(
                    0.07
                ),
                in:
                    Capsule()
            )


        } else {

            Text(
                "Offen"
            )
            .font(
                .system(
                    size: 10,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                Color.orange
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
                Color.orange.opacity(
                    0.07
                ),
                in:
                    Capsule()
            )
        }
    }


    // MARK: - Loading

    private var loadingView: some View {

        VStack(
            spacing: 14
        ) {

            ProgressView()
                .controlSize(
                    .large
                )


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
            90
        )
    }


    // MARK: - Error

    private func errorView(
        _ message: String
    ) -> some View {

        VStack(
            alignment: .leading,
            spacing: 14
        ) {

            Label(
                message,
                systemImage:
                    "exclamationmark.circle.fill"
            )
            .font(
                .subheadline
            )
            .foregroundStyle(
                .red
            )


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
                .fontWeight(
                    .semibold
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
            Color.red.opacity(
                0.055
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 17,
                    style: .continuous
                )
        )
    }


    // MARK: - Empty State

    private func emptyState(
        icon: String,
        title: String,
        message: String
    ) -> some View {

        VStack(
            spacing: 12
        ) {

            Image(
                systemName:
                    icon
            )
            .font(
                .system(
                    size: 27,
                    weight: .medium
                )
            )
            .foregroundStyle(
                Color.accentColor
            )


            Text(
                title
            )
            .font(
                .headline
            )


            Text(
                message
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
        .frame(
            maxWidth: .infinity
        )
        .padding(
            .vertical,
            30
        )
        .padding(
            .horizontal,
            22
        )
        .background(
            Color(
                uiColor:
                    .systemBackground
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 18,
                    style: .continuous
                )
        )
    }


    // MARK: - Load Tasks

    @MainActor
    private func loadTasks()
        async {

        guard !isLoading
        else {

            return
        }


        guard let currentSession =
            sessionManager.session
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

            let loadedModules =
                try await ModuleService()
                    .getAll(
                        accessToken:
                            currentSession
                                .accessToken
                    )


            try Task
                .checkCancellation()


            var loadedGlobalTasks:
                [GlobalTaskItem] = []


            try await withThrowingTaskGroup(
                of:
                    (
                        StudyModule,
                        [StudyTask]
                    ).self
            ) { group in

                for module in
                    loadedModules {

                    group.addTask {

                        let tasks =
                            try await StudyTaskService()
                                .getByModule(
                                    moduleId:
                                        module.id,
                                    accessToken:
                                        currentSession
                                            .accessToken
                                )


                        return (
                            module,
                            tasks
                        )
                    }
                }


                for try await result
                    in group {

                    let (
                        module,
                        tasks
                    ) = result


                    loadedGlobalTasks.append(
                        contentsOf:
                            tasks.map {

                                GlobalTaskItem(
                                    task:
                                        $0,
                                    module:
                                        module
                                )
                            }
                    )
                }
            }


            try Task
                .checkCancellation()


            sessionManager
                .checkExpiration()


            guard
                sessionManager
                    .session?
                    .accessToken
                    == currentSession
                        .accessToken

            else {

                return
            }


            animateContent =
                false


            modules =
                loadedModules

            globalTasks =
                loadedGlobalTasks

            hasLoaded =
                true


            await Task.yield()


            withAnimation(
                .spring(
                    response: 0.40,
                    dampingFraction: 0.88
                )
            ) {

                animateContent =
                    true
            }


        } catch is CancellationError {

            return


        } catch let moduleError
            as ModuleServiceError {

            if case .unauthorized =
                moduleError {

                sessionManager
                    .invalidateSession(
                        accessToken:
                            currentSession
                                .accessToken
                    )

                return
            }


            errorMessage =
                moduleError
                    .localizedDescription


        } catch let taskError
            as StudyTaskServiceError {

            if case .unauthorized =
                taskError {

                sessionManager
                    .invalidateSession(
                        accessToken:
                            currentSession
                                .accessToken
                    )

                return
            }


            errorMessage =
                taskError
                    .localizedDescription


        } catch {

            errorMessage =
                "Die Aufgaben konnten nicht geladen werden. "
                + "Bitte versuche es erneut."
        }
    }


    // MARK: - Sort Group

    private func sortGroup(
        _ task: StudyTask
    ) -> Int {

        if isOverdue(
            task
        ) {

            return 0
        }


        if !task.isCompleted {

            return 1
        }


        return 2
    }


    // MARK: - Überfällig

    private func isOverdue(
        _ task: StudyTask
    ) -> Bool {

        guard
            !task.isCompleted,
            let dueDate =
                task.dueDate

        else {

            return false
        }


        return dueDate < Date()
    }


    // MARK: - Filter Count

    private func count(
        for filter: TaskFilter
    ) -> Int {

        switch filter {

        case .all:

            return globalTasks.count


        case .open:

            return globalTasks.filter {

                !$0.task.isCompleted
                && !isOverdue(
                    $0.task
                )
            }
            .count


        case .overdue:

            return globalTasks.filter {

                isOverdue(
                    $0.task
                )
            }
            .count


        case .completed:

            return globalTasks.filter {

                $0.task.isCompleted
            }
            .count
        }
    }


    // MARK: - Module Code

    private func moduleCode(
        _ module: StudyModule
    ) -> String {

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


    // MARK: - Module Color

    private func moduleColor(
        _ hex: String?
    ) -> Color {

        guard
            let hex,
            let parsed =
                Color(
                    globalTaskHex:
                        hex
                )

        else {

            return .accentColor
        }


        return parsed
    }


    // MARK: - Datum

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
        globalTaskHex hex: String
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
