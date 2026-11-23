import { useMsal } from '@azure/msal-react'
import { AppShell, Button, Group, Text, Title } from '@mantine/core'

export function SignedInLayout() {
  const { instance } = useMsal()
  const account = instance.getActiveAccount()

  return (
    <AppShell header={{ height: 56 }}>
      <AppShell.Header px="md">
        <Group h="100%" justify="space-between">
          <Title order={3}>Throughline</Title>
          <Group gap="md">
            <Text>Signed in as {account?.name ?? account?.username}</Text>
            <Button variant="default" onClick={() => void instance.logoutRedirect({ account })}>
              Sign out
            </Button>
          </Group>
        </Group>
      </AppShell.Header>
    </AppShell>
  )
}
