import { Button } from '@fluentui/react-components'
import { CenteredMessage } from './CenteredMessage'

/** Shown when a screen fails while rendering, instead of a blank page. */
export function CrashPage() {
  return (
    <CenteredMessage
      title="Something went wrong"
      message="Reload the page to try again."
      action={<Button appearance="primary" onClick={() => window.location.reload()}>Reload</Button>}
    />
  )
}
