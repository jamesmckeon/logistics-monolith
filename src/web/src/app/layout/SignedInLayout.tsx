import { useMsal } from '@azure/msal-react'
import { Button, Text, makeStyles, tokens } from '@fluentui/react-components'

const useStyles = makeStyles({
  page: { minHeight: '100vh', backgroundColor: tokens.colorNeutralBackground2 },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
    height: '48px',
    padding: `0 ${tokens.spacingHorizontalL}`,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  account: { display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM },
  signOut: { color: tokens.colorNeutralForegroundOnBrand },
})

export function SignedInLayout() {
  const styles = useStyles()
  const { instance } = useMsal()
  const account = instance.getActiveAccount()

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <Text weight="semibold" size={400}>Throughline</Text>
        <div className={styles.account}>
          <Text>Signed in as {account?.name ?? account?.username}</Text>
          <Button
            appearance="transparent"
            className={styles.signOut}
            onClick={() => void instance.logoutRedirect({ account })}
          >
            Sign out
          </Button>
        </div>
      </header>
    </div>
  )
}
