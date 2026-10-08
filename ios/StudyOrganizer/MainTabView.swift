import SwiftUI


struct MainTabView: View {

    let session: AuthSession


    @State
    private var selectedTab: AppTab = .dashboard


    private enum AppTab: Int, Hashable, CaseIterable {

        case dashboard = 0
        case modules = 1
        case moodle = 2
        case tasks = 3
        case profile = 4


        var title: String {

            switch self {

            case .dashboard:
                return "Dashboard"

            case .modules:
                return "Module"

            case .moodle:
                return "Moodle"

            case .tasks:
                return "Aufgaben"

            case .profile:
                return "Profil"
            }
        }


        var icon: String {

            switch self {

            case .dashboard:
                return "rectangle.grid.2x2.fill"

            case .modules:
                return "books.vertical.fill"

            case .moodle:
                return "graduationcap.fill"

            case .tasks:
                return "checkmark.square.fill"

            case .profile:
                return "person.fill"
            }
        }
    }


    var body: some View {

        TabView(
            selection: $selectedTab
        ) {

            // MARK: - Dashboard

            NavigationStack {

                PlaceholderView(
                    title: "Dashboard",
                    subtitle:
                        "Behalte deine Lernmodule und Aufgaben im Blick.",
                    icon:
                        "rectangle.grid.2x2.fill"
                )
            }
            .tag(
                AppTab.dashboard
            )
            .toolbar(
                .hidden,
                for: .tabBar
            )


            // MARK: - Module

            ModulesView(
                session: session
            )
            .tag(
                AppTab.modules
            )
            .toolbar(
                .hidden,
                for: .tabBar
            )


            // MARK: - Moodle

            NavigationStack {

                PlaceholderView(
                    title: "Moodle",
                    subtitle:
                        "Deine Moodle-Kurse werden hier angezeigt.",
                    icon:
                        "graduationcap.fill"
                )
            }
            .tag(
                AppTab.moodle
            )
            .toolbar(
                .hidden,
                for: .tabBar
            )


            // MARK: - Aufgaben

            NavigationStack {

                PlaceholderView(
                    title: "Aufgaben",
                    subtitle:
                        "Hier entsteht deine modulübergreifende Aufgabenübersicht.",
                    icon:
                        "checkmark.square.fill"
                )
            }
            .tag(
                AppTab.tasks
            )
            .toolbar(
                .hidden,
                for: .tabBar
            )


            // MARK: - Profil

            NavigationStack {

                PlaceholderView(
                    title: "Profil",
                    subtitle:
                        "Profil und Einstellungen werden hier angezeigt.",
                    icon:
                        "person.fill"
                )
            }
            .tag(
                AppTab.profile
            )
            .toolbar(
                .hidden,
                for: .tabBar
            )
        }
        .safeAreaInset(
            edge: .bottom,
            spacing: 0
        ) {

            customTabBar
        }
    }


    // MARK: - Custom Tab Bar

    private var customTabBar: some View {

        GeometryReader { geometry in

            let totalWidth =
                geometry.size.width

            let horizontalPadding:
                CGFloat = 8

            let availableWidth =
                totalWidth
                - horizontalPadding * 2

            let itemWidth =
                availableWidth
                / CGFloat(
                    AppTab.allCases.count
                )


            ZStack(
                alignment: .leading
            ) {

                // MARK: Haupt-Glasfläche

                RoundedRectangle(
                    cornerRadius: 28,
                    style: .continuous
                )
                .fill(
                    .regularMaterial
                )


                RoundedRectangle(
                    cornerRadius: 28,
                    style: .continuous
                )
                .fill(
                    LinearGradient(
                        colors: [
                            Color.white.opacity(0.26),
                            Color.white.opacity(0.08),
                            Color.black.opacity(0.04)
                        ],
                        startPoint: .top,
                        endPoint: .bottom
                    )
                )


                // MARK: Bewegliche Auswahl-Kapsel

                RoundedRectangle(
                    cornerRadius: 22,
                    style: .continuous
                )
                .fill(
                    .ultraThinMaterial
                )
                .overlay {

                    RoundedRectangle(
                        cornerRadius: 22,
                        style: .continuous
                    )
                    .fill(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(0.34),
                                Color.white.opacity(0.12),
                                Color.accentColor.opacity(0.05)
                            ],
                            startPoint: .top,
                            endPoint: .bottom
                        )
                    )
                }
                .overlay {

                    RoundedRectangle(
                        cornerRadius: 22,
                        style: .continuous
                    )
                    .stroke(
                        Color.white.opacity(0.55),
                        lineWidth: 1
                    )
                }
                .shadow(
                    color:
                        Color.black.opacity(0.10),
                    radius: 8,
                    x: 0,
                    y: 4
                )
                .frame(
                    width: itemWidth - 4,
                    height: 56
                )
                .offset(
                    x:
                        horizontalPadding
                        + CGFloat(
                            selectedTab.rawValue
                        ) * itemWidth
                        + 2
                )
                .animation(
                    .spring(
                        response: 0.25,
                        dampingFraction: 0.86
                    ),
                    value: selectedTab
                )


                // MARK: Tabs

                HStack(
                    spacing: 0
                ) {

                    ForEach(
                        AppTab.allCases,
                        id: \.self
                    ) { tab in

                        tabButton(
                            tab
                        )
                        .frame(
                            width: itemWidth
                        )
                    }
                }
                .padding(
                    .horizontal,
                    horizontalPadding
                )
            }
            .overlay {

                RoundedRectangle(
                    cornerRadius: 28,
                    style: .continuous
                )
                .stroke(
                    Color.white.opacity(0.42),
                    lineWidth: 1
                )
            }
            .overlay(
                alignment: .top
            ) {

                RoundedRectangle(
                    cornerRadius: 28,
                    style: .continuous
                )
                .stroke(
                    Color.white.opacity(0.60),
                    lineWidth: 1
                )
                .mask {

                    LinearGradient(
                        colors: [
                            .white,
                            .clear
                        ],
                        startPoint: .top,
                        endPoint: .center
                    )
                }
            }
            .shadow(
                color:
                    Color.black.opacity(0.14),
                radius: 18,
                x: 0,
                y: 8
            )
        }
        .frame(
            height: 72
        )
        .padding(
            .horizontal,
            12
        )
        .padding(
            .top,
            8
        )
        .padding(
            .bottom,
            6
        )
    }


    // MARK: - Tab Button

    private func tabButton(
        _ tab: AppTab
    ) -> some View {

        let isSelected =
            selectedTab == tab


        return Button {

            withAnimation(
                .spring(
                    response: 0.25,
                    dampingFraction: 0.86
                )
            ) {

                selectedTab =
                    tab
            }

        } label: {

            VStack(
                spacing: 4
            ) {

                Image(
                    systemName:
                        tab.icon
                )
                .font(
                    .system(
                        size: 24,
                        weight:
                            isSelected
                            ? .semibold
                            : .medium
                    )
                )


                Text(
                    tab.title
                )
                .font(
                    .system(
                        size: 11,
                        weight:
                            isSelected
                            ? .semibold
                            : .medium
                    )
                )
                .lineLimit(
                    1
                )
                .minimumScaleFactor(
                    0.80
                )
            }
            .foregroundStyle(
                isSelected
                    ? Color.accentColor
                    : Color.secondary
            )
            .frame(
                maxWidth: .infinity
            )
            .frame(
                height: 56
            )
            .contentShape(
                Rectangle()
            )
        }
        .buttonStyle(
            .plain
        )
        .accessibilityLabel(
            tab.title
        )
        .accessibilityAddTraits(
            isSelected
                ? .isSelected
                : []
        )
    }
}


// MARK: - Placeholder View

private struct PlaceholderView: View {

    let title: String
    let subtitle: String
    let icon: String


    var body: some View {

        ScrollView {

            VStack(
                spacing: 18
            ) {

                Image(
                    systemName:
                        icon
                )
                .font(
                    .system(
                        size: 34,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    Color.accentColor
                )
                .frame(
                    width: 76,
                    height: 76
                )
                .background(
                    Color.accentColor
                        .opacity(
                            0.10
                        ),
                    in:
                        RoundedRectangle(
                            cornerRadius: 20,
                            style: .continuous
                        )
                )


                VStack(
                    spacing: 7
                ) {

                    Text(
                        title
                    )
                    .font(
                        .title2.bold()
                    )


                    Text(
                        subtitle
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
                .horizontal,
                24
            )
            .padding(
                .top,
                70
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
            title
        )
        .navigationBarTitleDisplayMode(
            .large
        )
    }
}
