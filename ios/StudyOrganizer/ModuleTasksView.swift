import SwiftUI

private enum TaskFilter: String, CaseIterable, Identifiable {

    case all = "Alle"
    case open = "Offen"
    case overdue = "Überfällig"
    case completed = "Erledigt"


    var id: Self {
        self
    }
}


struct ModuleTasksView: View {

    let module: StudyModule
    let session: AuthSession


    @EnvironmentObject
    private var sessionManager: SessionManager


    @State
    private var tasks: [StudyTask] = []


    @State
    private var isLoading = false

    @State
    private var hasLoaded = false

    @State
    private var errorMessage: String?

    @State
    private var showCreateTask = false

    @State
    private var updatingTaskID: String?

    @State
    private var statusErrorMessage: String?

    @State
    private var taskToEdit: StudyTask?
    
    @State
    private var taskToDelete: StudyTask?

    @State
    private var deletingTaskID: String?

    @State
    private var deleteErrorMessage: String?
    
    @State
    private var selectedFilter: TaskFilter = .all
    
    private var sortedTasks: [StudyTask] {

        tasks.sorted { lhs, rhs in

            let lhsGroup = sortGroup(
                for: lhs
            )

            let rhsGroup = sortGroup(
                for: rhs
            )


            if lhsGroup != rhsGroup {

                return lhsGroup < rhsGroup
            }


            switch (
                lhs.dueDate,
                rhs.dueDate
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


            return lhs.title.localizedCaseInsensitiveCompare(
                rhs.title
            ) == .orderedAscending
        }
    }
    
    private var visibleTasks: [StudyTask] {

        sortedTasks.filter { task in

            switch selectedFilter {

            case .all:

                return true


            case .open:

                return !task.isCompleted


            case .overdue:

                return isOverdue(
                    task
                )


            case .completed:

                return task.isCompleted
            }
        }
    }


    var body: some View {

        ScrollView {

            LazyVStack(
                spacing: 14
            ) {

                header
                
                
                if hasLoaded
                    && !tasks.isEmpty {

                    filterBar
                }


                if isLoading
                    && tasks.isEmpty {

                    loadingView
                }


                if let errorMessage {

                    errorCard(
                        errorMessage
                    )
                }


                if let statusErrorMessage {

                    Text(
                        statusErrorMessage
                    )
                    .font(
                        .subheadline
                    )
                    .foregroundStyle(
                        .red
                    )
                    .frame(
                        maxWidth: .infinity,
                        alignment: .leading
                    )
                }
                
                if let deleteErrorMessage {

                    Text(
                        deleteErrorMessage
                    )
                    .font(
                        .subheadline
                    )
                    .foregroundStyle(
                        .red
                    )
                    .frame(
                        maxWidth: .infinity,
                        alignment: .leading
                    )
                }

                if hasLoaded
                    && tasks.isEmpty
                    && !isLoading {

                    emptyState
                }
                
                if hasLoaded
                    && !tasks.isEmpty
                    && visibleTasks.isEmpty
                    && !isLoading {

                    filterEmptyState
                }


                ForEach(
                    visibleTasks
                ) { task in

                    TaskCardView(
                        task: task,
                        module: module,
                        style: .management,
                        onToggleStatus: {

                            Task {

                                await toggleStatus(
                                    for: task
                                )
                            }
                        },
                        onEdit: {

                            taskToEdit =
                                task
                        },
                        onDelete: {

                            taskToDelete =
                                task
                        }
                    )
                    .disabled(
                        updatingTaskID != nil
                        || deletingTaskID != nil
                    )
                    .confirmationDialog(
                        "„\(task.title)“ löschen?",
                        isPresented:
                            Binding(
                                get: {

                                    taskToDelete?.id
                                        == task.id
                                },
                                set: { isPresented in

                                    if !isPresented,
                                       taskToDelete?.id
                                        == task.id {

                                        taskToDelete =
                                            nil
                                    }
                                }
                            ),
                        titleVisibility:
                            .visible
                    ) {

                        Button(
                            "Löschen",
                            role: .destructive
                        ) {

                            Task {

                                await deleteTask(
                                    task
                                )
                            }
                        }

                    } message: {

                        Text(
                            "Diese Aufgabe wird dauerhaft gelöscht."
                        )
                    }
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

        // MARK: Aufgabe erstellen

        .sheet(
            isPresented:
                $showCreateTask
        ) {

            CreateTaskView(
                module: module,
                session: session
            ) { createdTask in

                tasks.removeAll {
                    $0.id == createdTask.id
                }


                tasks.insert(
                    createdTask,
                    at: 0
                )


                hasLoaded =
                    true

                errorMessage =
                    nil
            }
            .environmentObject(
                sessionManager
            )
        }

        // MARK: Aufgabe bearbeiten

        .sheet(
            item:
                $taskToEdit
        ) { task in

            EditTaskView(
                module: module,
                task: task,
                session: session
            ) { updatedTask in

                if let index =
                    tasks.firstIndex(
                        where: {
                            $0.id
                                == updatedTask.id
                        }
                    ) {

                    tasks[index] =
                        updatedTask
                }


                errorMessage =
                    nil

                statusErrorMessage =
                    nil
            }
            .environmentObject(
                sessionManager
            )
        }
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


            Button {

                showCreateTask =
                    true

            } label: {

                Image(
                    systemName:
                        "plus"
                )
                .font(
                    .system(
                        size: 18,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .white
                )
                .frame(
                    width: 42,
                    height: 42
                )
                .background(
                    Color.accentColor,
                    in:
                        Circle()
                )
            }
            .buttonStyle(
                .plain
            )
            .accessibilityLabel(
                "Neue Aufgabe erstellen"
            )
        }
    }
    
    // MARK: - Filter

    private var filterBar: some View {

        ScrollView(
            .horizontal,
            showsIndicators: false
        ) {

            HStack(
                spacing: 8
            ) {

                ForEach(
                    TaskFilter.allCases
                ) { filter in

                    Button {

                        withAnimation(
                            .easeInOut(
                                duration: 0.18
                            )
                        ) {

                            selectedFilter =
                                filter
                        }

                    } label: {

                        Text(
                            filter.rawValue
                        )
                        .font(
                            .system(
                                size: 14,
                                weight: .semibold
                            )
                        )
                        .foregroundStyle(
                            selectedFilter == filter
                                ? Color.white
                                : Color.primary
                        )
                        .padding(
                            .horizontal,
                            14
                        )
                        .frame(
                            height: 36
                        )
                        .background(
                            selectedFilter == filter
                                ? Color.accentColor
                                : Color(
                                    uiColor:
                                        .secondarySystemBackground
                                ),
                            in:
                                Capsule()
                        )
                        .overlay {

                            if selectedFilter != filter {

                                Capsule()
                                    .stroke(
                                        Color.secondary
                                            .opacity(
                                                0.16
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
            }
        }
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
    
    private var filterEmptyState: some View {

        VStack(
            spacing: 14
        ) {

            Image(
                systemName:
                    filterEmptyIcon
            )
            .font(
                .system(
                    size: 30,
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
                    filterEmptyTitle
                )
                .font(
                    .headline
                )


                Text(
                    "Für diesen Filter wurden keine Aufgaben gefunden."
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
                    style: .continuous
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


        guard
            sessionManager
                .session?
                .accessToken
                == session.accessToken

        else {

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


            guard
                sessionManager
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

            guard
                !Task.isCancelled,

                sessionManager
                    .session?
                    .accessToken
                    == session.accessToken

            else {

                return
            }


            if let serviceError =
                error
                    as?
                        StudyTaskServiceError,

               case .unauthorized =
                serviceError {

                tasks =
                    []


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


    // MARK: - Status ändern

    @MainActor
    private func toggleStatus(
        for task: StudyTask
    ) async {

        guard updatingTaskID == nil
        else {

            return
        }


        guard let currentSession =
                sessionManager.session
        else {

            errorMessage =
                "Deine Sitzung ist nicht mehr gültig. "
                + "Bitte melde dich erneut an."

            return
        }


        updatingTaskID =
            task.id

        statusErrorMessage =
            nil


        let newStatus:
            StudyTaskStatus =
                task.isCompleted
                ? .open
                : .completed


        do {

            let updatedTask =
                try await StudyTaskService()
                    .updateStatus(
                        moduleId:
                            module.id,
                        taskId:
                            task.id,
                        status:
                            newStatus,
                        accessToken:
                            currentSession
                                .accessToken
                    )


            if let index =
                tasks.firstIndex(
                    where: {
                        $0.id
                            == task.id
                    }
                ) {

                tasks[index] =
                    updatedTask
            }


            updatingTaskID =
                nil


        } catch is CancellationError {

            updatingTaskID =
                nil


        } catch let serviceError
            as StudyTaskServiceError {

            updatingTaskID =
                nil


            if case .unauthorized =
                serviceError {

                sessionManager
                    .invalidateSession(
                        accessToken:
                            currentSession
                                .accessToken
                    )

                return
            }


            statusErrorMessage =
                serviceError
                    .localizedDescription


        } catch {

            updatingTaskID =
                nil

            statusErrorMessage =
                "Der Aufgabenstatus konnte "
                + "nicht geändert werden. "
                + "Bitte versuche es erneut."
        }
    }
    
    @MainActor
    private func deleteTask(
        _ task: StudyTask
    ) async {

        guard deletingTaskID == nil
        else {

            return
        }


        guard let currentSession =
                sessionManager.session
        else {

            deleteErrorMessage =
                "Deine Sitzung ist nicht mehr gültig. "
                + "Bitte melde dich erneut an."

            return
        }


        deletingTaskID =
            task.id

        deleteErrorMessage =
            nil


        defer {

            deletingTaskID =
                nil

            taskToDelete =
                nil
        }


        do {

            try await StudyTaskService()
                .delete(
                    moduleId:
                        module.id,
                    taskId:
                        task.id,
                    accessToken:
                        currentSession
                            .accessToken
                )


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


            tasks.removeAll {
                $0.id == task.id
            }


            hasLoaded =
                true


        } catch is CancellationError {

            return


        } catch let serviceError
            as StudyTaskServiceError {

            if case .unauthorized =
                serviceError {

                sessionManager
                    .invalidateSession(
                        accessToken:
                            currentSession
                                .accessToken
                    )

                return
            }


            deleteErrorMessage =
                serviceError
                    .localizedDescription


        } catch {

            deleteErrorMessage =
                "Die Aufgabe konnte nicht gelöscht werden. "
                + "Bitte versuche es erneut."
        }
    }

    // MARK: - Helpers

    private func color(
        for hex: String?
    ) -> Color {

        guard
            let hex,

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

        guard
            let value,

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


    private func formattedDueDate(
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
            from: date
        )
    }
    
    private func sortGroup(
        for task: StudyTask
    ) -> Int {

        if task.isCompleted {

            return 2
        }


        if isOverdue(
            task
        ) {

            return 0
        }


        return 1
    }
    
    private var filterEmptyTitle: String {

        switch selectedFilter {

        case .all:

            return "Keine Aufgaben"


        case .open:

            return "Keine offenen Aufgaben"


        case .overdue:

            return "Keine überfälligen Aufgaben"


        case .completed:

            return "Keine erledigten Aufgaben"
        }
    }
    
    private var filterEmptyIcon: String {

        switch selectedFilter {

        case .all:

            return "checklist"


        case .open:

            return "circle"


        case .overdue:

            return "clock.badge.exclamationmark"


        case .completed:

            return "checkmark.circle"
        }
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
