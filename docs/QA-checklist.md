PLANET SCRAPPERS - QA CHECKLIST
==============================

How to test
-----------
1. Extract the zip anywhere (for example your Desktop).
2. Open the extracted folder and run PlanetScrappers.exe. Unity is not needed.
3. The version is shown in the bottom-left corner of the screen. Put it in every bug report.
4. Mark each test Pass / Fail. For a failure, add a bug report (template below) with a screenshot or video.

Tests marked [M1] can be tested in the Module 1 build. The others become testable in later modules.

ID      Module  Scenario                                Expected result
------  ------  --------------------------------------  ------------------------------------------------------------
QA-001  [M1]    Launch the game                         Game opens full screen with no error. (Main menu arrives in Module 4.)
QA-002  [M1]    Start in Rust Valley                    Player stands outside the base facing north into the valley.
QA-003  [M1]    Move: WASD, Shift to sprint, Space      Smooth movement, sprint is faster, jump is slightly floaty (low gravity).
QA-003b [M1]    Look with the mouse                     Camera turns smoothly, cannot flip upside down.
QA-003c [M1]    Esc, then click                         Esc frees the mouse and shows a hint; clicking resumes play.
QA-003d [M1b]   Press V                                 View switches between third-person and first-person.
QA-003e [M1b]   Walk and jump in third-person           Rookie walks with legs and arms swinging, leaves footprints and dust.
QA-003f [M1b]   Walk close to a building or rock        Camera moves in so the view is never blocked.
QA-004  [M2]    Hit an object with the wrench           Object takes damage, durability bar drops, sound and sparks play.
QA-005  [M2]    Break an object                         Object breaks apart and is removed.
QA-006  [M2]    Collect scrap                           Scrap flies to the player; backpack count increases.
QA-007  [M3]    Sell at the Recycling Depot             Credits increase by the shown total; backpack empties.
QA-008  [M3]    Buy the Scrap Hammer                    Credits decrease; hammer is equipped and breaks objects faster.
QA-009  [M4]    Save, quit, relaunch, Continue          Position, credits, inventory, tools and missions are restored.
QA-010  [M1]    Walk into the perimeter fence           Player cannot leave the playable area.
QA-011  [M4]    Pause and resume                        Game freezes while paused and continues correctly.
QA-012  [M4]    Lowest graphics settings                Game stays playable and readable.
QA-013  [M1]    General feel                            Note anything that feels too big, too small, too slow or too fast.

Also record
-----------
- Windows version, CPU, GPU and RAM of the test PC.
- Average frame rate shown in the corner while walking around.

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
