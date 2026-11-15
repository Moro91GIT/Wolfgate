# Wolfgate admin ship panel

## Opening it
wf-ship-admin-button = Ship Panel
wf-ship-admin-verb = Ship Panel
wf-ship-admin-verb-description = Open the ship panel on { $ship }.
cmd-shipadmin-desc = Opens the admin ship panel, on a ship when one is named.
cmd-shipadmin-help = Usage: { $command } [grid entity | here]
cmd-shipadmin-not-grid = "{ $grid }" isn't a grid.
cmd-shipadmin-hint-grid = [grid entity | here]

## Window
wf-ship-admin-title = Ship Panel
wf-ship-admin-search = Search ships, owners or designs...
wf-ship-admin-refresh = Refresh
wf-ship-admin-filter-ships = Player ships
wf-ship-admin-filter-all = All grids
wf-ship-admin-empty = No ships found.
wf-ship-admin-count = Showing { $shown } of { $total }
wf-ship-admin-count-truncated = Showing { $shown } of the first { $total }; search to narrow it
wf-ship-admin-none-selected = Pick a ship from the list.
wf-ship-admin-row = { $owner } { $state ->
        [online] (online)
        [offline] (offline)
       *[none] { "" }
    } | aboard: { $aboard }
wf-ship-admin-summary = Owner: { $owner } | { $map } | aboard: { $aboard }
wf-ship-admin-unowned = no owner
wf-ship-admin-none = none
wf-ship-admin-locked = Locked
wf-ship-admin-unlocked = Unlocked
wf-ship-admin-needs-flag = Needs the { $flag } permission.

wf-ship-admin-confirm = Confirm?
wf-ship-admin-notify = Tell the owners
wf-ship-admin-notify-tooltip = Sends the old and new owners a chat line saying staff changed the ship. Untick it for a quiet fix; the admin log records it either way.
wf-ship-admin-unclaimed = Unclaimed

## Tabs
wf-ship-admin-tab-overview = Overview
wf-ship-admin-tab-ownership = Ownership

## Tags
wf-ship-admin-tag-locked = locked
wf-ship-admin-tag-docked = docked
wf-ship-admin-tag-inftl = in FTL
wf-ship-admin-tag-snapshot = has repair snapshot
wf-ship-admin-tag-crewlinked = crew ship
wf-ship-admin-tag-company = company ship
wf-ship-admin-tag-hangar = hangar ship
wf-ship-admin-tag-fixing = being fixed

## Access modes and owner kinds
wf-ship-admin-mode-private = Private
wf-ship-admin-mode-faction = Faction
wf-ship-admin-mode-crew = Crew
wf-ship-admin-mode-company = Company
wf-ship-admin-owner-deedcard = Deed card
wf-ship-admin-owner-voucher = Deed voucher
wf-ship-admin-owner-account = Account
wf-ship-admin-owner-unstamped = { $name } (no player on record)
wf-ship-admin-owner-stamped = { $name } ({ $username }, { $online ->
        [true] online
       *[false] offline
    })

## Overview tab
wf-ship-admin-scan = Scan value
wf-ship-admin-scan-tooltip = Appraise the ship. Slow on a large hull, so it only runs when asked.
wf-ship-admin-overview-ship = Ship
wf-ship-admin-overview-design = Design
wf-ship-admin-overview-entity = Entity
wf-ship-admin-overview-place = Position
wf-ship-admin-overview-place-value = { $map } ({ $x }, { $y })
wf-ship-admin-overview-mass = Mass
wf-ship-admin-overview-docked = Docked to
wf-ship-admin-overview-value = Value
wf-ship-admin-overview-value-amount = { $value } spesos
wf-ship-admin-overview-not-scanned = not scanned
wf-ship-admin-overview-state = State
wf-ship-admin-overview-owners = Owners
wf-ship-admin-overview-record-owner = Named owner
wf-ship-admin-overview-registered = Shipyard account
wf-ship-admin-overview-account = { $name } ({ $online ->
        [true] online
       *[false] offline
    })
wf-ship-admin-overview-no-owners = Nobody holds the deed and no account is registered.
wf-ship-admin-overview-access = Access
wf-ship-admin-overview-no-access = This grid has no ship access.
wf-ship-admin-overview-mode = Mode
wf-ship-admin-overview-lock = Lock
wf-ship-admin-overview-allowed = Allow list
wf-ship-admin-overview-crew = Crew
wf-ship-admin-overview-company = Company
wf-ship-admin-overview-faction = Hull faction
wf-ship-admin-overview-aboard = Aboard ({ $count })
wf-ship-admin-overview-aboard-more = and { $count } more

## Ownership tab
wf-ship-admin-ownership-give = Hand the ship to a player
wf-ship-admin-ownership-give-note = A transfer takes the ship from every current owner. The new owner gets the deed on their ID card when they carry one without a deed; otherwise the ship is registered to their account alone.
wf-ship-admin-ownership-nobody-picked = Pick a player above.
wf-ship-admin-ownership-picked = Picked: { $name } ({ $username })
wf-ship-admin-ownership-keep-previous = Keep previous owners as crew
wf-ship-admin-ownership-keep-previous-tooltip = Puts each ID card that held the deed on the allow list, so the old owners can still open the doors.
wf-ship-admin-ownership-clear-allow = Clear the allow list
wf-ship-admin-ownership-clear-allow-tooltip = Removes everyone the old owner let aboard. Left unticked, their guests keep their access.
wf-ship-admin-ownership-drop-company = Drop the company link
wf-ship-admin-ownership-drop-company-tooltip = Stops the ship flying for the old owner's player company. It is replaced by the new owner's company when their ID card has one.
wf-ship-admin-ownership-leave-crew = Take it out of Crew mode
wf-ship-admin-ownership-leave-crew-tooltip = The linked crew keeps spawning aboard, but its cards stop opening the doors.
wf-ship-admin-ownership-detach-hangar = Detach from the company hangar
wf-ship-admin-ownership-detach-hangar-tooltip = A hangar ship belongs to its company and can't change hands until it is detached.
wf-ship-admin-ownership-faction-keep = Hull faction: keep
wf-ship-admin-ownership-faction-fromcard = Hull faction: from the new owner's ID
wf-ship-admin-ownership-faction-clear = Hull faction: none
wf-ship-admin-ownership-transfer = Transfer ownership
wf-ship-admin-ownership-add = Add as co-owner
wf-ship-admin-ownership-add-tooltip = Registers the ship to their account as well. Nobody loses it.
wf-ship-admin-ownership-remove = Remove
wf-ship-admin-ownership-remove-last = The last owner can't be removed. Release the ship instead.
wf-ship-admin-ownership-release-heading = Leave it with no owner
wf-ship-admin-ownership-release-note = Every owner loses the ship and its doors unlock. Its helm stays locked, so only staff fly it until it is transferred.
wf-ship-admin-ownership-release = Release ownership
wf-ship-admin-label-previous-owner = Previous owner

## Chat lines to players
wf-ship-admin-notice-lost = Staff have taken { $ship } out of your ownership.
wf-ship-admin-notice-gained = Staff have made you an owner of { $ship }.

## Results
wf-ship-admin-result-no-permission = You don't have the permission for that.
wf-ship-admin-result-gone = That ship no longer exists.
wf-ship-admin-result-unknown = The panel can't do that yet.
wf-ship-admin-result-scanned = Appraised { $ship }.
wf-ship-admin-result-not-ship = That grid isn't a ship, so it can't have an owner.
wf-ship-admin-result-offline = That player isn't connected. A ship can only be handed to someone who is.
wf-ship-admin-result-hangar = That is a company hangar ship. Tick "Detach from the company hangar", which needs the Debug permission.
wf-ship-admin-result-transfer-failed = The deed could not be assigned.
wf-ship-admin-result-transferred = Transferred { $ship } from { $from } to { $to }.
wf-ship-admin-result-already-owner = { $name } already owns it.
wf-ship-admin-result-owner-added = { $name } now co-owns { $ship }.
wf-ship-admin-result-not-owner = That owner is no longer on the ship.
wf-ship-admin-result-last-owner = That is the last owner. Release the ship instead.
wf-ship-admin-result-owner-removed = Removed { $name } as an owner of { $ship }.
wf-ship-admin-result-no-owner = That ship has no owner to release it from.
wf-ship-admin-result-released = Released { $ship }. It has no owner now.
wf-ship-admin-note-account-only = Registered to their account only: { $reason }
wf-ship-admin-note-crew = It is the ship of crew "{ $crew }", which keeps spawning aboard.
wf-ship-admin-note-unlocked = It was unlocked, as no owner's card could open it yet; it locks itself once one can.
wf-ship-admin-note-untold = Not connected, so not told: { $names }.
