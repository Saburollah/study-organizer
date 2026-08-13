export default {
  navigation: {
    mainLabel: 'Main navigation',
    home: 'Home',
    dashboard: 'Dashboard',
    modules: 'Study modules',
    profile: 'Profile',
    login: 'Sign in',
    register: 'Sign up',
    logout: 'Sign out',
    language: 'Select language',
    german: 'German',
    english: 'English',
  },

  home: {
    hero: {
      eyebrow: 'YOUR STUDIES. YOUR PLAN.',
      title: 'More clarity.',
      titleAccent: 'Less stress.',
      description:
        'Organize study modules, tasks, and deadlines in one place—clearly, personally, and always up to date.',
      dashboard: 'Go to dashboard',
      openModules: 'Open study modules',
      startFree: 'Get started for free',
      existingAccount: 'I already have an account',
    },

    benefits: {
      label: 'Benefits',
      deadlines: 'Keep track of deadlines',
      onePlace: 'Everything in one place',
      secure: 'Secure and personal',
    },

    preview: {
      label: 'Study Organizer preview',
      week: 'MY WEEK',
      greeting: 'Good morning',
      overview: 'Your overview',
      progressLabel: '75 percent completed',
      modules: 'Study modules',
      open: 'Open',
      completed: 'Completed',
      next: 'Up next',
      today: 'Today',
      tomorrow: 'Tomorrow',
      mathematics: 'Mathematics',
      mathTask: 'Exercise sheet 6',
      softwareEngineering: 'Software Engineering',
      softwareTask: 'Complete C4 diagram',
      organized: 'Well organized',
      completedThisWeek: '3 tasks completed this week',
    },

    features: {
      eyebrow: 'EVERYTHING AT A GLANCE',
      title: 'Your tools for a well-organized semester',
      description:
        'Spend less time switching between apps—and more time on what really matters.',

      modules: {
        title: 'Study modules',
        description:
          'Organize lectures and courses clearly with individual colors and abbreviations.',
        detail: 'Structure for every semester',
      },

      tasks: {
        title: 'Tasks',
        description:
          'Plan your next steps, set deadlines, and easily mark completed work.',
        detail: 'Never miss a deadline',
      },

      progress: {
        title: 'Progress',
        description:
          'See immediately what is open, overdue, or already completed.',
        detail: 'Stay motivated through clarity',
      },
    },

    callToAction: {
      eyebrow: 'READY FOR MORE CLARITY?',
      title: 'Your semester starts with a clear plan.',
      dashboard: 'Open dashboard',
      createAccount: 'Create account',
    },
  },
} as const
