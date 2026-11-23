import { Button, Center, Stack, Text, Title } from '@mantine/core'

/** Shown when a screen fails while rendering, instead of a blank page. */
export function CrashPage() {
  return (
    <Center mih="100vh">
      <Stack align="center" gap="md">
        <Title order={2}>Something went wrong</Title>
        <Text>Reload the page to try again.</Text>
        <Button onClick={() => window.location.reload()}>Reload</Button>
      </Stack>
    </Center>
  )
}
