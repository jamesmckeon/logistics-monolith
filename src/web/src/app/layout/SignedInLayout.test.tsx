import type { AccountInfo } from '@azure/msal-browser'
import { useMsal } from '@azure/msal-react'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../testing/render'
import { SignedInLayout } from './SignedInLayout'

vi.mock('@azure/msal-react')

const account = {
  name: 'Dana Reyes',
  username: 'dana.reyes@throughline.example',
} as AccountInfo

function givenSignedIn(signedInAccount: AccountInfo) {
  const logoutRedirect = vi.fn().mockResolvedValue(undefined)
  vi.mocked(useMsal).mockReturnValue({
    instance: { getActiveAccount: () => signedInAccount, logoutRedirect },
  } as unknown as ReturnType<typeof useMsal>)
  return { logoutRedirect }
}

describe('SignedInLayout', () => {
  it('shows who the person is signed in as', () => {
    givenSignedIn(account)

    render(<SignedInLayout />)

    expect(screen.getByText('Signed in as Dana Reyes')).toBeInTheDocument()
  })

  it('signs the person out of their own account when they click Sign out', async () => {
    const { logoutRedirect } = givenSignedIn(account)
    render(<SignedInLayout />)

    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }))

    expect(logoutRedirect).toHaveBeenCalledExactlyOnceWith({ account })
  })
})
