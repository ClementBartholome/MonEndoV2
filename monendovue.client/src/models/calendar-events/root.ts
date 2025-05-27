import type { Item } from './item'

export interface Root {
    kind: string
    etag: string
    summary: string
    description: string
    updated: string
    timeZone: string
    accessRole: string
    defaultReminders: any[]
    nextSyncToken: string
    items: Item[]
}