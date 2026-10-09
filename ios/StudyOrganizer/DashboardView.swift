import SwiftUI


struct DashboardView: View {

    let session: AuthSession
    
    let refreshID: Int


    @EnvironmentObject
    private var sessionManager: SessionManager


    @State
    private var modules: [StudyModule] = []


    @State
    private var dashboardTasks: [DashboardTask] = []


    @State
    private var isLoading = false


    @State
    private var hasLoaded = false


    @State
    private var errorMessage: String?


    @State
    private var animateContent = false


    // MARK: - Dashboard Task

    private struct DashboardTask: Identifiable {

        let task: StudyTask
        let module: StudyModule


        var id: String {
            task.id
        }
    }


    // MARK: - Statistik

    private var openTaskCount: Int {

        dashboardTasks.filter {
            !$0.task.isCompleted
        }
        .count
    }


    private var overdueTaskCount: Int {

        dashboardTasks.filter {
            isOverdue(
                $0.task
            )
        }
        .count
    }


    private var completedTaskCount: Int {

        dashboardTasks.filter {
            $0.task.isCompleted
        }
        .count
    }


    // MARK: - Nächste Aufgaben

    private var nextTasks: [DashboardTask] {

        dashboardTasks
            .filter {
                !$0.task.isCompleted
            }
            .sorted { lhs, rhs in

                let lhsOverdue =
                    isOverdue(
                        lhs.task
                    )

                let rhsOverdue =
                    isOverdue(
                        rhs.task
                    )


                if lhsOverdue != rhsOverdue {

                    return lhsOverdue
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
            .prefix(5)
            .map {
                $0
            }
    }


    // MARK: - Body

    var body: some View {

        ScrollView {

            LazyVStack(
                alignment: .leading,
                spacing: 20
            ) {

                dashboardHeader


                if isLoading
                    && !hasLoaded {

                    loadingView


                } else if let errorMessage {

                    errorView(
                        errorMessage
                    )


                } else {

                    overviewSection


                    nextTasksSection
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
                135
            )
        }
        .background(
            dashboardBackground
        )
        .navigationTitle(
            "Dashboard"
        )
        .navigationBarTitleDisplayMode(
            .large
        )
        .refreshable {

            await loadDashboard()
        }
        .task(
            id: refreshID
        ) {

            await loadDashboard()
        }
    }


    // MARK: - Hintergrund

    private var dashboardBackground: some View {

        ZStack {

            Color(
                uiColor:
                    .systemGroupedBackground
            )


            LinearGradient(
                colors: [
                    Color.accentColor
                        .opacity(
                            0.035
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

    private var dashboardHeader: some View {

        VStack(
            alignment: .leading,
            spacing: 6
        ) {

            Text(
                "DEIN ÜBERBLICK"
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
                "Behalte deine Lernmodule "
                + "und Aufgaben im Blick."
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
            .fixedSize(
                horizontal: false,
                vertical: true
            )
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
    }


    // MARK: - Übersicht

    private var overviewSection: some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            sectionHeader(
                eyebrow:
                    "AKTUELL",
                title:
                    "Übersicht"
            )


            LazyVGrid(
                columns: [
                    GridItem(
                        .flexible(),
                        spacing: 12
                    ),
                    GridItem(
                        .flexible(),
                        spacing: 12
                    )
                ],
                spacing: 12
            ) {

                statisticCard(
                    title:
                        "Lernmodule",
                    value:
                        modules.count,
                    icon:
                        "books.vertical.fill",
                    color:
                        .blue,
                    index:
                        0
                )


                statisticCard(
                    title:
                        "Offen",
                    value:
                        openTaskCount,
                    icon:
                        "circle",
                    color:
                        .orange,
                    index:
                        1
                )


                statisticCard(
                    title:
                        "Überfällig",
                    value:
                        overdueTaskCount,
                    icon:
                        "exclamationmark.triangle.fill",
                    color:
                        .red,
                    index:
                        2
                )


                statisticCard(
                    title:
                        "Erledigt",
                    value:
                        completedTaskCount,
                    icon:
                        "checkmark.circle.fill",
                    color:
                        .green,
                    index:
                        3
                )
            }
        }
    }


    // MARK: - Section Header

    private func sectionHeader(
        eyebrow: String,
        title: String
    ) -> some View {

        VStack(
            alignment: .leading,
            spacing: 3
        ) {

            Text(
                eyebrow
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


            Text(
                title
            )
            .font(
                .system(
                    size: 22,
                    weight: .bold
                )
            )
        }
    }


    // MARK: - Statistik Card

    private func statisticCard(
        title: String,
        value: Int,
        icon: String,
        color: Color,
        index: Int
    ) -> some View {

        VStack(
            alignment: .leading,
            spacing: 14
        ) {

            Image(
                systemName:
                    icon
            )
            .font(
                .system(
                    size: 22,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                color
            )
            .shadow(
                color:
                    color.opacity(
                        0.10
                    ),
                radius: 3,
                y: 2
            )


            Spacer(
                minLength: 0
            )


            VStack(
                alignment: .leading,
                spacing: 2
            ) {

                Text(
                    "\(value)"
                )
                .font(
                    .system(
                        size: 30,
                        weight: .bold,
                        design: .rounded
                    )
                )
                .foregroundStyle(
                    .primary
                )


                Text(
                    title
                )
                .font(
                    .system(
                        size: 13.5,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    .secondary
                )
            }
        }
        .frame(
            maxWidth: .infinity,
            minHeight: 106,
            alignment: .leading
        )
        .padding(
            15
        )
        .background {

            RoundedRectangle(
                cornerRadius: 21,
                style: .continuous
            )
            .fill(
                .regularMaterial
            )


            RoundedRectangle(
                cornerRadius: 21,
                style: .continuous
            )
            .fill(
                LinearGradient(
                    colors: [
                        color.opacity(
                            0.09
                        ),
                        color.opacity(
                            0.025
                        ),
                        Color.white.opacity(
                            0.025
                        )
                    ],
                    startPoint:
                        .topLeading,
                    endPoint:
                        .bottomTrailing
                )
            )
        }
        .overlay {

            RoundedRectangle(
                cornerRadius: 21,
                style: .continuous
            )
            .stroke(
                Color.white.opacity(
                    0.46
                ),
                lineWidth: 1
            )
        }
        .overlay(
            alignment: .top
        ) {

            RoundedRectangle(
                cornerRadius: 21,
                style: .continuous
            )
            .stroke(
                Color.white.opacity(
                    0.68
                ),
                lineWidth: 1
            )
            .mask {

                LinearGradient(
                    colors: [
                        .white,
                        .clear
                    ],
                    startPoint:
                        .top,
                    endPoint:
                        .center
                )
            }
        }
        .shadow(
            color:
                color.opacity(
                    0.055
                ),
            radius: 8,
            x: 0,
            y: 4
        )
        .shadow(
            color:
                Color.black.opacity(
                    0.045
                ),
            radius: 10,
            x: 0,
            y: 5
        )
        .scaleEffect(
            animateContent
                ? 1
                : 0.95
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
                : 8
        )
        .animation(
            .spring(
                response: 0.40,
                dampingFraction: 0.87
            )
            .delay(
                Double(index)
                    * 0.045
            ),
            value:
                animateContent
        )
    }


    // MARK: - Nächste Aufgaben

    private var nextTasksSection: some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            sectionHeader(
                eyebrow:
                    "ALS NÄCHSTES",
                title:
                    "Nächste Aufgaben"
            )


            if modules.isEmpty {

                dashboardEmptyState(
                    icon:
                        "books.vertical",
                    title:
                        "Noch keine Lernmodule",
                    message:
                        "Erstelle dein erstes Lernmodul, "
                        + "um dein Dashboard zu füllen."
                )


            } else if nextTasks.isEmpty {

                dashboardEmptyState(
                    icon:
                        "checkmark.circle",
                    title:
                        "Keine offenen Aufgaben",
                    message:
                        "Aktuell steht nichts an."
                )


            } else {

                VStack(
                    spacing: 10
                ) {

                    ForEach(
                        Array(
                            nextTasks.enumerated()
                        ),
                        id:
                            \.element.id
                    ) { index, item in

                        dashboardTaskCard(
                            item,
                            index:
                                index
                        )
                    }
                }
            }
        }
    }


    // MARK: - Dashboard Task Card

    private func dashboardTaskCard(
        _ item: DashboardTask,
        index: Int
    ) -> some View {

        let overdue =
            isOverdue(
                item.task
            )


        let accent =
            overdue
            ? Color.red
            : moduleColor(
                item.module.color
            )


        return HStack(
            spacing: 12
        ) {

            RoundedRectangle(
                cornerRadius: 4,
                style: .continuous
            )
            .fill(
                accent
            )
            .frame(
                width: 4
            )


            VStack(
                alignment: .leading,
                spacing: 7
            ) {

                HStack(
                    alignment: .center
                ) {

                    Text(
                        moduleCode(
                            item.module
                        )
                    )
                    .font(
                        .system(
                            size: 10.5,
                            weight: .bold
                        )
                    )
                    .foregroundStyle(
                        moduleColor(
                            item.module.color
                        )
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
                        moduleColor(
                            item.module.color
                        )
                        .opacity(
                            0.08
                        ),
                        in:
                            Capsule()
                    )


                    Spacer()


                    if overdue {

                        Text(
                            "Überfällig"
                        )
                        .font(
                            .system(
                                size: 10.5,
                                weight: .semibold
                            )
                        )
                        .foregroundStyle(
                            .red
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
                            Color.red
                                .opacity(
                                    0.08
                                ),
                            in:
                                Capsule()
                        )
                    }
                }


                Text(
                    item.task.title
                )
                .font(
                    .system(
                        size: 16,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    .primary
                )
                .lineLimit(
                    2
                )


                Text(
                    item.module.name
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


                if let dueDate =
                    item.task.dueDate {

                    Label {

                        Text(
                            formattedDate(
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
                            size: 12,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(
                        overdue
                        ? Color.red
                        : Color.secondary
                    )
                    .padding(
                        .top,
                        1
                    )
                }
            }
            .padding(
                .vertical,
                13
            )
            .padding(
                .trailing,
                14
            )
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
        .background {

            RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
            .fill(
                Color(
                    uiColor:
                        .systemBackground
                )
            )


            RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
            .fill(
                LinearGradient(
                    colors: [
                        Color.white.opacity(
                            0.07
                        ),
                        accent.opacity(
                            overdue
                                ? 0.018
                                : 0.009
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
        .clipShape(
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
                        0.09
                    ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.black.opacity(
                    0.03
                ),
            radius: 7,
            y: 3
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
                : 9
        )
        .animation(
            .spring(
                response: 0.42,
                dampingFraction: 0.88
            )
            .delay(
                0.18
                + Double(index)
                * 0.04
            ),
            value:
                animateContent
        )
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
                "Dashboard wird geladen …"
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


    // MARK: - Fehler

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

                    await loadDashboard()
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

    private func dashboardEmptyState(
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


    // MARK: - Daten laden

    @MainActor
    private func loadDashboard()
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


            var loadedTasks:
                [DashboardTask] = []


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


                    loadedTasks.append(
                        contentsOf:
                            tasks.map {

                                DashboardTask(
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

            dashboardTasks =
                loadedTasks

            hasLoaded =
                true


            await Task.yield()


            withAnimation(
                .spring(
                    response: 0.42,
                    dampingFraction: 0.87
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
                "Das Dashboard konnte nicht geladen werden. "
                + "Bitte versuche es erneut."
        }
    }


    // MARK: - Helpers

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


    private func moduleColor(
        _ hex: String?
    ) -> Color {

        guard
            let hex,
            let parsedColor =
                Color(
                    dashboardHex:
                        hex
                )

        else {

            return .accentColor
        }


        return parsedColor
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


// MARK: - Dashboard Hex

private extension Color {

    init?(
        dashboardHex hex: String
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
