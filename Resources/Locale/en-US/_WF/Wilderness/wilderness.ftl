## Zones

wf-zone-safe-title = SAFE ZONE
wf-zone-safe-summary = Policy protects you here: no theft, no unprovoked attacks. Cause trouble and you lose that protection.
wf-zone-safe-entered = You entered the {$place} Safe Zone. {$summary}
wf-zone-safe-left = You left the {$place} Safe Zone. Ordinary escalation rules apply.
wf-zone-safe-ring = SAFE ZONE

wf-zone-outpost-title = PROTECTED OUTPOST
wf-zone-outpost-summary = Ceasefire: this outpost, and ships within 256 m of it, may not be struck while the war is COLD.
wf-zone-outpost-hot-title = OUTPOST AT WAR
wf-zone-outpost-hot-summary = The war is HOT: this outpost, and ships within 256 m of it, may be struck.
wf-zone-outpost-entered = You are within 256 m of {$place}. {$summary}
wf-zone-outpost-left = You left the outpost zone of {$place}.
wf-zone-outpost-ring = OUTPOST

wf-zone-wilderness-title = WILDERNESS: NO PROTECTION
wf-zone-wilderness-summary = { $open ->
    [yes] Anyone may attack anyone here without escalation. Stay and you are marked.
   *[no] TSF and PDV may fight here without escalation; everyone else still needs it. Stay and you are marked.
}
wf-zone-wilderness-entered = You entered a WILDERNESS round {$place}. {$summary}
wf-zone-wilderness-left = You left the Wilderness round {$place}.
wf-zone-wilderness-ring = WILDERNESS

wf-zone-notsafe-title = NOT A SAFE ZONE
wf-zone-notsafe-summary = This station is not a Safe Zone. Ordinary rules and escalation apply.
wf-zone-notsafe-mobile-summary = A mobile base: not protected from port strikes, though the ceasefire still holds while the war is COLD.
wf-zone-notsafe-entered = You are aboard {$place}. {$summary}
wf-zone-notsafe-left = You left {$place}.
wf-zone-notsafe-ring = NOT SAFE

wf-zone-chat-wrap = [color={$color}][bold]{$message}[/bold][/color]

## The Wilderness mark

wf-zone-skull-marked = { $ship ->
    [yes] Your ship is now MARKED.
   *[no] You are now MARKED.
} { $open ->
    [yes] Anyone
   *[no] TSF and PDV
} may engage { $ship ->
    [yes] it
   *[no] you
} without escalation, anywhere, Safe Zone included, until the mark runs out, {$minutes} minutes after you leave the Wilderness.
wf-zone-skull-leaving = You left the Wilderness. { $ship ->
    [yes] Your ship stays
   *[no] You stay
} MARKED for {$minutes} minutes: no protection anywhere, Safe Zone included.
wf-zone-skull-expired = { $ship ->
    [yes] Your ship's mark has run out. Ordinary rules protect it again.
   *[no] Your mark has run out. Ordinary rules protect you again.
}

## Banner and titles

wf-zone-banner-marked = { $ship ->
    [yes] YOUR SHIP IS MARKED
   *[no] YOU ARE MARKED
}: {$time} left. No protection anywhere, Safe Zone included.
wf-zone-banner-marked-held = { $ship ->
    [yes] YOUR SHIP IS MARKED
   *[no] YOU ARE MARKED
} while you stay here, and for {$minutes} minutes after you leave.
wf-zone-banner-marking = { $ship ->
    [yes] Your ship is marked
   *[no] You are marked
} in {$seconds} s. Leave now to avoid it.
wf-zone-banner-overridden = Your mark overrides this Safe Zone: you have no protection here.
wf-zone-banner-hint = Click for the rules.
wf-zone-pill-marked = MARKED {$time}
wf-zone-pill-marked-held = MARKED
wf-zone-pill-marking = Marked in {$seconds} s
wf-zone-banner-title = {$title}  ·  {$place}
wf-zone-title-entering = ENTERING {$title}
wf-zone-title-leaving = LEAVING {$title}
wf-zone-title-marked = MARKED
wf-zone-title-marked-sub = No protection anywhere until the mark runs out.

## Radar

wf-zone-radar-marked = MARKED

## Alerts

wf-alert-zone-safe-name = Safe Zone
wf-alert-zone-safe-desc = Policy protects you here: no theft, no unprovoked attacks. Cause trouble and you lose that protection.
wf-alert-zone-outpost-name = Faction outpost
wf-alert-zone-outpost-desc = Within 256 m of a faction outpost: protected from port strikes while the war is COLD, open to them while it is HOT.
wf-alert-zone-wilderness-name = Wilderness
wf-alert-zone-wilderness-desc = No policy protection: fighting here needs no escalation. Stay and you, or your ship, are marked; the sweep over this icon counts down to it.
wf-alert-zone-notsafe-name = Not a Safe Zone
wf-alert-zone-notsafe-desc = This station is not a Safe Zone. Ordinary rules and escalation apply.
wf-alert-marked-name = Marked
wf-alert-marked-desc = You, or your ship, stayed in a Wilderness. Until the mark runs out you have no protection anywhere, Safe Zone included. Others see the skull on radar. It holds while you stay; once you leave, the sweep over this icon counts down what is left.

## Guidebook

guide-entry-wf-wilderness = Zones and the Wilderness
wf-wilderness-rule-who = { $open ->
    [yes] -- [bold]Anyone may fight anyone[/bold] in a Wilderness without escalation.
   *[no] -- [bold]TSF and PDV may fight each other[/bold] in a Wilderness without escalation, even while COLD. Everyone else still needs escalation.
}
wf-wilderness-rule-mark = { $onfoot ->
    [yes] -- A ship, or a person not aboard one,
   *[no] -- A ship
} that stays [bold]{$seconds} seconds[/bold] in a Wilderness is [color=#d93c3c][bold]marked[/bold][/color]: a skull on every radar, and over a marked person's head. The mark holds while it stays and runs out [bold]{$minutes} minutes[/bold] after it leaves. Those who may fight in a Wilderness may engage a marked ship or person [bold]anywhere, Safe Zone included[/bold], until then.

## Options

ui-options-wf-zones = Zones
ui-options-wf-wilderness-border = Coloured screen border in a Wilderness and while marked
ui-options-wf-wilderness-rings = Zone edges on radars

## Admin command

cmd-wf_zone-desc = Shows your zone and Wilderness mark, or sets or clears the mark.
cmd-wf_zone-help = Usage: wf_zone [info|mark|clear]
cmd-wf_zone-hint = info, mark or clear
cmd-wf_zone-no-body = You need a body.
cmd-wf_zone-held = held (in a Wilderness)
cmd-wf_zone-info = Zone: {$zone}. Carrier: {$carrier}. Mark: {$mark}.
cmd-wf_zone-marked = Marked {$carrier}.
cmd-wf_zone-cleared = Cleared the mark on {$carrier}.
