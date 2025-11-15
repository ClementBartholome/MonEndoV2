import type { Creator } from './creator'
import type { Organizer } from './organizer'
import type { Start } from './start'
import type { End } from './end'
import type { Attendee } from './attendee'

export interface Item {
    kind: string
    etag: string
    id: string
    status: string
    htmlLink: string
    created?: string
    updated: string
    summary?: string
    creator?: Creator
    organizer?: Organizer
    start: Start
    end: End
    iCalUID: string
    sequence?: number
    attendees?: Attendee[]
    eventType?: string
    description?: string
    guestsCanInviteOthers?: boolean
    location?: string
    privateCopy?: boolean
    transparency?: string
    visibility?: string
}