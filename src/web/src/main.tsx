import '@mantine/core/styles.css'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './app/App'
import { createAuthClient } from './lib/auth'

const rootElement = document.getElementById('root')

if (rootElement === null) {
  throw new Error('index.html has no element with id "root".')
}

const authClient = await createAuthClient()

createRoot(rootElement).render(
  <StrictMode>
    <App authClient={authClient} />
  </StrictMode>,
)
