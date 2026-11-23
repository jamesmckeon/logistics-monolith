import type { IPublicClientApplication } from '@azure/msal-browser'
import { AuthenticatedTemplate, MsalProvider, UnauthenticatedTemplate } from '@azure/msal-react'
import { MantineProvider } from '@mantine/core'
import { ErrorBoundary } from 'react-error-boundary'
import { CrashPage } from './layout/CrashPage'
import { SignedInLayout } from './layout/SignedInLayout'
import { SignInPage } from './layout/SignInPage'

type AppProps = {
  authClient: IPublicClientApplication
}

export function App({ authClient }: AppProps) {
  return (
    <MantineProvider>
      <ErrorBoundary fallback={<CrashPage />}>
        <MsalProvider instance={authClient}>
          <AuthenticatedTemplate>
            <SignedInLayout />
          </AuthenticatedTemplate>
          <UnauthenticatedTemplate>
            <SignInPage />
          </UnauthenticatedTemplate>
        </MsalProvider>
      </ErrorBoundary>
    </MantineProvider>
  )
}
