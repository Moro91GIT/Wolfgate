wf-judgeline-program-name = Judge Line
wf-encounter-name-judge-responder = Judge responder {$designation}

wf-judgeline-kind-underfire = Under fire
wf-judgeline-kind-boarded = Boarded
wf-judgeline-kind-medical = Medical
wf-judgeline-kind-stranded = Stranded
wf-judgeline-kind-adrift = Adrift
wf-judgeline-kind-assault = Assault
wf-judgeline-kind-underfire-hint = Another ship is shooting at yours.
wf-judgeline-kind-boarded-hint = Hostiles are aboard your ship.
wf-judgeline-kind-medical-hint = You or your crew are badly hurt or dead.
wf-judgeline-kind-stranded-hint = Your ship has no thrust, fuel or power.
wf-judgeline-kind-adrift-hint = You're in space with no ship.
wf-judgeline-kind-assault-hint = You're being attacked in person.

wf-judgeline-app-title = Judge Line
wf-judgeline-app-intro = Call a Caelestinus judge ship to an emergency. Vagrants only, Colossus sector only. Costs {$fee} spesos. If the judges find no emergency, you lose the fee and can't call again for a while.
wf-judgeline-app-cancel = Cancel call
wf-judgeline-app-queued = Waiting for a free ship (place {$place})
wf-judgeline-app-dispatched = Judges arriving in {$seconds}s
wf-judgeline-app-on-scene = Judges on scene
wf-judgeline-app-open = [bold]{$kind}[/bold]: {$phase}
    {$status}

wf-judgeline-refuse-offline = The Judge Line is closed.
wf-judgeline-refuse-not-vagrant = The Judge Line only takes calls from vagrants.
wf-judgeline-refuse-open = You already have a call open.
wf-judgeline-refuse-sector = The judges only answer in the Colossus sector.
wf-judgeline-refuse-station = You're on a station. Get help there.
wf-judgeline-refuse-barred = Too many false calls. The Judge Line won't take yours this shift.
wf-judgeline-refuse-cooldown = You can call again in {$minutes} min.
wf-judgeline-refuse-warrant = You owe the Bench. Pay your tickets first.
wf-judgeline-refuse-funds = A call costs {$fee} spesos.

wf-judgeline-chat = [color=#37946e][bold]Judge Line:[/bold][/color] {$message}
wf-judgeline-status-received = Call received: {$kind}. Waiting for a free ship.
wf-judgeline-status-dispatched = A judge ship is on its way. ETA {$seconds} seconds.
wf-judgeline-status-inbound = The judge ship is out of FTL and closing on you.
wf-judgeline-status-on-scene = The judges are on scene.
wf-judgeline-status-switched = The judges found a different emergency and are dealing with it: {$kind}.
wf-judgeline-status-aboard = You're aboard. The judges are taking you to {$station}.
wf-judgeline-status-disembark = Docked. Please get off the judge ship.
wf-judgeline-status-relief = The judges left you a relief crate with fuel and repair supplies. Their engineer is coming aboard to fuel your generator.
wf-judgeline-status-refuelled = The judges fuelled your generator.
wf-judgeline-status-resolved = Emergency over. The judges are leaving. Stay safe, vagrant.
wf-judgeline-status-timeout = The judges can't stay any longer and are leaving.
wf-judgeline-status-hoax = The judges found no emergency. Your fee is kept. False calls: {$strikes} of {$limit}.
wf-judgeline-status-betrayed = Your ship fired on the judges. The call is void.
wf-judgeline-status-cancelled = Call cancelled.
wf-judgeline-status-left = You left the sector. The call is void.
wf-judgeline-status-lost = The judge ship was lost.
wf-judgeline-status-unavailable = No judge ship could reach you. Your fee is refunded.

wf-judgeline-radio-inbound = Judge responder out of FTL, inbound to your position.
wf-judgeline-radio-underfire = Judges on scene. Hostile ship, break off now or be fired on.
wf-judgeline-radio-boarded = Judges docking. Boarding party, stand down.
wf-judgeline-radio-medical = Judges docking with a paramedic aboard.
wf-judgeline-radio-stranded = Judges docking with relief supplies.
wf-judgeline-radio-adrift = Judges alongside. Get to our airlock, we'll take you in.
wf-judgeline-radio-assault = Judges on scene. Whoever's attacking that vagrant, back off.
wf-judgeline-radio-hoax = No emergency here. Don't waste the judges' time.
wf-judgeline-radio-leaving = Judges leaving. Fly safe.

wf-judgeline-verb-category = Judge Line
wf-judgeline-verb-cancel = Cancel call
wf-judgeline-verb-confirm = Call the judges for {$fee} spesos?

cmd-wf_judgeline-desc = Lists Judge Line calls, places a free one, or ends one.
cmd-wf_judgeline-help = Usage: {$command} list | call <kind> [player] | end <id>
cmd-wf_judgeline-row = #{$id} {$caller}: {$kind}, {$phase}
cmd-wf_judgeline-no-call = No such call.
cmd-wf_judgeline-bad-kind = Kinds: UnderFire, Boarded, Medical, Stranded, Adrift, Assault.
cmd-wf_judgeline-no-caller = No caller: attach to an entity or name a player.
cmd-wf_judgeline-placed = Placed call #{$id}.
