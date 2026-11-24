import { useMsal } from '@azure/msal-react'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../testing/render'
import { SignInPage } from './SignInPage'

vi.mock('@azure/msal-react')

function givenAuthClient() {
  const loginRedirect = vi.fn().mockResolvedValue(undefined)
  vi.mocked(useMsal).mockReturnValue({
    instance: { loginRedirect },
  } as unknown as ReturnType<typeof useMsal>)
  return { loginRedirect }
}

describe('SignInPage', () => {
  it('sends the person to Entra sign-in when they click Sign in', async () => {
    const { loginRedirect } = givenAuthClient()
    render(<SignInPage />)

    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(loginRedirect).toHaveBeenCalledOnce()
  })
})
