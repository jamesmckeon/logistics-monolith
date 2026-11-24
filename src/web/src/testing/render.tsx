import { FluentProvider } from '@fluentui/react-components'
import { render as testingLibraryRender } from '@testing-library/react'
import type { ReactNode } from 'react'
import { theme } from '../app/theme'

/** Renders inside FluentProvider with Throughline's theme, as the app does. */
export function render(ui: ReactNode) {
  return testingLibraryRender(ui, {
    wrapper: ({ children }: { children: ReactNode }) => <FluentProvider theme={theme}>{children}</FluentProvider>,
  })
}
