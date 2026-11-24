import { useMsal } from '@azure/msal-react'
import { Button } from '@fluentui/react-components'
import { CenteredMessage } from './CenteredMessage'

export function SignInPage() {
  const { instance } = useMsal()

  return (
    <CenteredMessage
      title="Throughline"
      message="Sign in with your Throughline account to continue."
      action={<Button appearance="primary" onClick={() => void instance.loginRedirect()}>Sign in</Button>}
    />
  )
}
