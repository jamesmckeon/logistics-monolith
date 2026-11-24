function required(name: string, value: string | undefined): string {
  if (value === undefined || value.trim() === '') {
    throw new Error(`Setting ${name} is missing. Set it in this environment's .env file.`)
  }

  return value
}

/** Settings for the environment this build is for, from its .env file. */
export const env = {
  entraTenantId: required('VITE_ENTRA_TENANT_ID', import.meta.env.VITE_ENTRA_TENANT_ID),
  entraClientId: required('VITE_ENTRA_CLIENT_ID', import.meta.env.VITE_ENTRA_CLIENT_ID),
} as const
