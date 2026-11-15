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

## Tabs
wf-ship-admin-tab-overview = Overview

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

## Results
wf-ship-admin-result-no-permission = You don't have the permission for that.
wf-ship-admin-result-gone = That ship no longer exists.
wf-ship-admin-result-unknown = The panel can't do that yet.
wf-ship-admin-result-scanned = Appraised { $ship }.
