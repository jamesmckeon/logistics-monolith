import { type AccountInfo, EventType, PublicClientApplication } from '@azure/msal-browser'
import { env } from '../config/env'

/**
 * Creates the sign-in client for this environment's Entra registration. Entra returns
 * sign-in and sign-out responses to redirect.html, which hands them back to the app.
 */
export async function createAuthClient(): Promise<PublicClientApplication> {
  const redirectUri = `${window.location.origin}/redirect.html`

  const client = new PublicClientApplication({
    auth: {
      clientId: env.entraClientId,
      authority: `https://login.microsoftonline.com/${env.entraTenantId}`,
      redirectUri,
      postLogoutRedirectUri: redirectUri,
    },
  })

  await client.initialize()

  client.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS) {
      client.setActiveAccount(event.payload as AccountInfo)
    }
  })

  return client
}
