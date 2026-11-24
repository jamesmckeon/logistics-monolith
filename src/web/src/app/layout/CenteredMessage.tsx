import { Body1, Title2, makeStyles, tokens } from '@fluentui/react-components'
import type { ReactNode } from 'react'

const useStyles = makeStyles({
  page: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingHorizontalL,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  card: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    maxWidth: '400px',
    padding: tokens.spacingHorizontalXXL,
    textAlign: 'center',
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusLarge,
    boxShadow: tokens.shadow4,
  },
})

type CenteredMessageProps = {
  title: string
  message: string
  action: ReactNode
}

/** A full-page card with a title, a line of text and one action, for pages with nothing else on them. */
export function CenteredMessage({ title, message, action }: CenteredMessageProps) {
  const styles = useStyles()

  return (
    <main className={styles.page}>
      <div className={styles.card}>
        <Title2 as="h1" block>{title}</Title2>
        <Body1 as="p" block>{message}</Body1>
        {action}
      </div>
    </main>
  )
}
