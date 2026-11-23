import { useMsal } from '@azure/msal-react'
import { Button, Center, Stack, Text, Title } from '@mantine/core'

export function SignInPage() {
  const { instance } = useMsal()

  return (
    <Center mih="100vh">
      <Stack align="center" gap="md">
        <Title order={1}>Throughline</Title>
        <Text>Sign in with your Throughline account to continue.</Text>
        <Button onClick={() => void instance.loginRedirect()}>Sign in</Button>
      </Stack>
    </Center>
  )
}
