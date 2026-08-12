export interface PasswordRequirement {
  key: string
  label: string
  isMet: boolean
}

export function getPasswordRequirements(
  password: string,
): PasswordRequirement[] {
  return [
    {
      key: 'length',
      label: 'mindestens 15 Zeichen',
      isMet: password.length >= 15,
    },
    {
      key: 'uppercase',
      label: 'mindestens ein Großbuchstabe (A–Z)',
      isMet: /[A-ZÄÖÜ]/.test(password),
    },
    {
      key: 'lowercase',
      label: 'mindestens ein Kleinbuchstabe (a–z)',
      isMet: /[a-zäöüß]/.test(password),
    },
    {
      key: 'digit',
      label: 'mindestens eine Ziffer (0–9)',
      isMet: /[0-9]/.test(password),
    },
    {
      key: 'special-character',
      label:
        'mindestens ein Sonderzeichen: @ # $ % & * - _ ! + = : , . ? / " ( ) ;',
      isMet:
        /[@#$%&*_!+=:,.?/"'();-]/.test(password),
    },
  ]
}

export function isPasswordValid(
  password: string,
): boolean {
  return getPasswordRequirements(password)
    .every((requirement) => requirement.isMet)
}