import { type BrandVariants, createLightTheme } from '@fluentui/react-components'

/**
 * Throughline's brand ramp, Mulberry: shade 10 darkest, 160 lightest. Fluent uses shade 80 for primary
 * buttons and the header, where white text has 7.3:1 contrast.
 */
const mulberry: BrandVariants = {
  10: '#200717',
  20: '#2d0d21',
  30: '#3b142c',
  40: '#4a1b38',
  50: '#592344',
  60: '#692a51',
  70: '#78325d',
  80: '#873c6a',
  90: '#97517b',
  100: '#a8668c',
  110: '#b87c9e',
  120: '#c892af',
  130: '#d8a8c2',
  140: '#e8bed4',
  150: '#f7d5e7',
  160: '#ffecfa',
}

export const theme = createLightTheme(mulberry)
