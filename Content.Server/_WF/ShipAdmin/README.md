# ShipAdmin

The admin ship panel: one window where staff see every ship and change one. It opens from the Ship Panel button
on the Wolfgate admin tab, with `shipadmin [grid|here]`, or from the Ship Panel verb (Admin category) on a grid
or anything standing on one, which opens it on that ship.

The left side lists ships: grids with a deed, an owner, ship access or a vessel design, or every grid with the
All grids filter (the first 500). The right side shows the selected ship in tabs. Overview is read-only: design,
position, what it is docked to, every owner (deed cards, vouchers and registered accounts, with who is online),
access mode and lock, and who is aboard; Scan value appraises it on request.

The Admin flag opens the panel. `WFShipAdminPermissions` is the one table of which flag each action needs; the
server checks it on every message and the window greys what the viewer may not do. Every action is written to
the admin log as `WFShipAdmin`.

Entry points: `WFShipAdminSystem` (`Open`, `BuildList`, `BuildDetail`), `WFShipAdminEui`, `WFShipAdminCommand`,
`WFShipAdminWindow`.

<!-- WOLFGATE-GENERATED START -->
<!-- WOLFGATE-GENERATED END -->
