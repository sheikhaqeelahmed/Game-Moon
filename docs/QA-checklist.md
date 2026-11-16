PLANET SCRAPPERS - QA CHECKLIST (v0.7.0)
========================================

How to test
-----------
1. Extract the zip anywhere (for example your Desktop).
2. Open the extracted folder and run PlanetScrappers.exe. Unity is not needed.
3. The version is shown in the bottom-left corner. Put it in every bug report.
4. Mark each test Pass / Fail. For a failure, add a bug report (template below) with a screenshot or video.
5. Play the first shift naturally first (about 20-30 minutes), then go through the list.

Controls
--------
WASD move · Mouse look · Shift sprint · Space jump · V switch third/first person
Left mouse: use tool · E: use / pick up · F: SCRAP-BOT scan · 1/2/3: switch tools
Tab: inventory · J: missions · Esc: pause / close

ID      Scenario                                   Expected result
------  -----------------------------------------  ------------------------------------------------------------
QA-001  Launch the game                            Main menu appears with New Game, Settings, Credits, Exit.
QA-002  New Game                                   Player stands outside the base in Rust Valley; Mission 1 shows; SCRAP-BOT speaks.
QA-003  Move, sprint, jump, look, V                Smooth movement, floaty low-gravity jump, view switches with V.
QA-004  Pick up the wrench (E at the crate)        Wrench appears on the salvage arm; slot 1 shows Rusty Wrench.
QA-005  Hit an oil drum (hold left mouse)          Arm swings, sparks and clank, durability bar drops, parts fall off at stages.
QA-006  Break the drum                             Drum breaks with a crunch; scrap pieces pop out.
QA-007  Walk near the scrap                        Scrap flies into the backpack; backpack count goes up.
QA-008  Hit a Mule-7 truck with the wrench         Bonk sound, no damage; SCRAP-BOT says a tier 2 tool is needed.
QA-009  F while aiming at an object                SCRAP-BOT reports tier, durability and contents.
QA-010  Walk into the perimeter fence              Player cannot leave the playable area.
QA-011  Fill the backpack                          Scrap stops being collected; SCRAP-BOT warns it is full.
QA-012  Sell at the Recycling Depot (E at hopper)  Sell screen shows totals; selling empties the backpack and adds credits.
QA-013  Buy the Scrap Hammer at Nova's (E)         Credits drop by 250; hammer fitted; trucks and containers can be dismantled.
QA-014  Buy a bigger backpack                      Capacity goes from 30 to 50.
QA-015  Complete all 8 missions                    Each mission pays its reward; Mission 8 opens the Machine Row gate.
QA-016  Dismantle a generator in Machine Row       Works with the Electric Dismantler only.
QA-017  Wait away from a broken object (5 min)     A fresh object appears in its place.
QA-018  Stay away from base until oxygen is low    Warning at 25%; at 0 health drains; at 0 health you return to base with half the scrap.
QA-019  Pause (Esc) and resume                     Game freezes while paused and continues correctly.
QA-020  Save Game, Exit, relaunch, Continue        Position, credits, backpack, tools, missions and broken objects are restored.
QA-021  Settings: change each option               Each change applies; settings are kept after restarting.
QA-022  Lowest graphics quality                    Game stays playable and readable.
QA-024  Get in the truck (E) and drive         Camera follows the truck; W/S/A/D/Space work; E gets out beside it.
QA-025  Drive over loose scrap                 Scrap flies into the truck bin; HUD shows bin count.
QA-026  Park near the depot and sell the load  Depot shows the truck bin and sells it.
QA-027  Build a Scrap Collector on a pad       Costs 800 cr; collector appears and pulls in scrap within 15 m.
QA-028  Store backpack at the warehouse        Backpack empties into the warehouse; depot can sell the warehouse.
QA-029  Enter the Iron District                Only reachable through Machine Row and the north gap.
QA-030  Dismantle a robot arm, then an         Robot arm needs tier 3; assembly line needs the Plasma Cutter (tier 4).
        assembly line
QA-031  Complete missions 9-14                 Each pays its reward; the last one ends all current contracts.
QA-032  Build the Recycling Factory (Nova's)   Factory appears south of the base; with scrap in the warehouse it pays credits.
QA-033  Switch the factory off (E at panel)    It stops working and stops charging.
QA-034  Buy a Salvage Robot                    A drone leaves the dock, dismantles junk and unloads at the warehouse.
QA-035  Build a conveyor link                  Crates move along the belt; the collector empties into the warehouse.
QA-036  Spaceport Graveyard gate               Opens once you own the Plasma Cutter (tier 4).
QA-037  Cut the freighter apart                Nose and wings with tier 4; hull and engine need the Crusher (tier 5).
QA-038  Deep Core gate and oxygen              Opens with the Crusher; oxygen drains much faster underground.
QA-039  Complete missions 15-20                Each pays its reward; the last says only the Mega-Factory remains.
QA-040  Mega-Factory gate                      Opens only after mission 20 with the Crusher.
QA-041  Hit the reactor core early             It is shielded; SCRAP-BOT says how many pylons remain.
QA-042  Dismantle the four pylons, then core   Shield bubble disappears; the core can be taken apart.
QA-043  Finish mission 23                      Ending screen with stats; Keep salvaging returns to play.
QA-044  Achievements                           Pop-up on unlock; Achievements screen lists them with dates.
QA-045  Objective marker                       Mission panel shows distance and direction to the target.
QA-046  Accessibility settings                 Text size, reduce motion, toggle tool use, message time all work.
QA-047  Gamepad in menus                       D-pad/stick moves between buttons, A selects, B/Start goes back.
QA-048  New character models                 Rookie walks, swings the arm and holds tools correctly; no gaps or floating parts.
QA-049  Nova and Captain Bolt                 Standing at their stations; heads turn toward you when you are close.
QA-050  SH-01 truck                           New model drives; the wheels spin the right way; the bin still fills.
QA-051  Brighter look                         Scene looks sunlit, not dark; nothing is washed out or too glowy.
QA-052  New junk models                        Drums, vending bots, scooters, containers, Mule-7 trucks and generators lose pieces as you hit them.
QA-053  Collisions on new models              You can't walk through any prop or building; nothing blocks you where nothing is visible.
QA-054  Base buildings                       Base, Upgrade Station, Depot and Warehouse look finished; signs are readable.
QA-055  Later-zone models                     Iron District, Spaceport, Deep Core and Mega-Factory objects look finished and lose pieces when hit.
QA-056  Reactor core and pylons               Pylons and core still work: shield drops after four pylons, core breaks after.
QA-023  General feel                               Note anything too slow, too fast, too hard, confusing or not fun.

Also record
-----------
- Windows version, CPU, GPU and RAM of the test PC.
- Average frame rate shown in the corner while playing.
- How long the first shift (all 8 missions) took you.

Bug report template
-------------------
Build version:
Test ID:
Issue title:
Steps to reproduce:
Expected result:
Actual result:
Severity (Low / Medium / High / Blocker):
Screenshot or video link:
Status (Open / Fixed / Retest / Closed):

Automated check (for developers)
--------------------------------
PlanetScrappers.exe -autotest plays the whole first shift by itself, logs 29 checks as [Autotest] PASS/FAIL
in the player log, saves screenshots to %USERPROFILE%\AppData\LocalLow\Planet Scrappers\Planet Scrappers\autotest,
and restores any existing save afterwards.
