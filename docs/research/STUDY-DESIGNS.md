# Study designs (draft proposals)

Three concrete designs mapped to this repository. They are drafts for discussion with collaborators, not approved protocols. Citations refer to entries in [README.md](README.md) and [references.bib](references.bib). All hypotheses below are hypotheses, not expected results; the literature suggests "about as good as" is a realistic outcome for several of them (Kaplan et al., 2021; Poyade et al., 2021; Moro et al., 2017).

## Shared elements

**Measures (all three studies).**

| Measure | Instrument or source | Notes |
|---|---|---|
| Knowledge test | Short multiple-choice and short-answer test written for each scenario, piloted before use | Pre-test, immediate post-test, and a delayed post-test (for example two weeks, following Buttussi & Chittaro, 2018). Reviews report that retention is rarely measured (Hamilton et al., 2021; Scorgie et al., 2024). |
| Transfer task | A hands-on task in a scenario the learner did not train on, or a physical or bench task where one is feasible | Reviews report that behavior-level outcomes are rarely measured (Stefan et al., 2023). Scoring rubric defined before data collection. |
| Time and errors | Logged by the app (see logging proposal) | Ordinary mistakes and critical misses counted separately. |
| Cybersickness | Simulator Sickness Questionnaire (Kennedy et al., 1993); background in Rebenitsch & Owen (2016) | Before and after exposure. Needed for the headset conditions (Moro et al., 2017 reported more adverse effects in a VR group). |
| Presence | Schubert, Friedmann, & Regenbrecht (2001) | Also relevant to Slater (2009). |
| Usability | System Usability Scale (Brooke, 1996) | Bibliographic details partly to confirm; see README. |
| Workload | NASA-TLX (Hart & Staveland, 1988) | Scoring variant to be chosen and stated before data collection. |

**Sample size.** No sample sizes are proposed here. A power analysis is needed once an effect size of interest and the planned analysis are chosen; effect sizes should be taken from a cited source or a pilot, not guessed. The Kaplan et al. (2021) meta-analysis and the reviews in the README are possible sources for plausible effect-size ranges, after reading the full texts.

**Anonymous logging proposal (not implemented).** The apps could write a local or relay-side log with no names or accounts: a random session code issued by the researcher, scenario id, condition id, build version, timestamps for step start and end, count and type of mistakes, critical misses (step id and time), hint use, and completion status. No video, audio, headset identifiers, or IP addresses would be stored. Whether this is adequate de-identification, where the data are stored, and the retention period are questions for the review process below. Implementing it would be a change to the apps and is outside this document.

**Ethics and review.** Collecting data from people requires review by the institution's human-subjects (IRB) process before any recruitment or data collection. Typical items to prepare for that review: the consent text, how participants are recruited and whether participation is voluntary and unrelated to grades, how data are stored and de-identified, and plans for participants who feel unwell in a headset. Students in the researcher's own courses are a vulnerable-recruitment situation to raise with the board; do not assume what the process allows. Scenario content is general safety awareness practice, not a certified procedure, and participant materials should say so.

## Study A: hands-on 3D tasks versus choose-a-response

**Maps to.** The Fieldwork XR Practice Lab (`training-simulations/`), which moved from a choose-a-response format to hands-on tasks in a 3D scene; the earlier format is in the repository's git history (a comparison build would need to be recovered or recreated, and tested).

**Research questions.**
1. Does a hands-on 3D task format lead to different knowledge scores than a choose-a-response format covering the same content?
2. Does it lead to different performance on a transfer task in an untrained scenario?
3. How do time, errors, workload, and usability differ?

**Hypotheses (to be tested, not assumed).**
- H1: Learners in the hands-on condition will score higher than the choose-a-response condition on the transfer task. The conceptual basis is the embodied-design argument (Johnson-Glenberg, 2018); the empirical comparison is open.
- H2: The two conditions will not differ much on an immediate knowledge test of facts (an "equivalence" expectation consistent with Kaplan et al., 2021).
- H3: The hands-on condition will report higher workload (NASA-TLX) and take longer; whether higher workload is helpful or harmful for learning is an open question (Sweller, 1988; Mayer & Moreno, 2003).

**Conditions.** (1) Hands-on 3D tasks. (2) Choose-a-response version with identical learning objectives and feedback content. Same scenarios, same order, same on-screen text where possible. Between-subjects, or within-subjects with counterbalanced scenarios (in which case scenario difficulty must be balanced).

**Measures.** Knowledge test (pre, post, delayed), transfer task, time, errors and critical misses, NASA-TLX, SUS, presence if the choose-a-response version is immersive (otherwise omit or interpret with care), SSQ for headset use.

**Limitations.** The choose-a-response version must be rebuilt or recovered and may differ in other ways (interface, feedback) from the hands-on version, which confounds the format. Short single-session training. Single institution and probably a convenience sample. A transfer task inside the same app may reward familiarity with the interface rather than understanding.

## Study B: low versus high visual realism for the same scenario

**Maps to.** Scenarios that already have a quality setting (for example the Hydrostatic Test Bay in `vive-steamvr/` and the WebXR examples).

**Research questions.**
1. Does higher visual realism change learning, presence, or workload for the same task?
2. Does realism interact with prior experience?

**Hypotheses.**
- H1: High realism will produce higher self-reported presence than low realism (Slater, 2009 distinguishes place illusion from plausibility; visual fidelity may affect the first more than the second).
- H2: High realism will not improve, and may reduce, knowledge and transfer scores, in line with the presence-versus-load pattern in Makransky et al. (2019). This is a hypothesis to test in a procedural-safety setting, not an expected result; Parong & Mayer (2018) is a related study to read first.
- H3: Workload will be higher with high realism.

**Conditions.** Low-fidelity and high-fidelity rendering of an identical scenario with identical task logic, timing, and feedback. Control for frame rate and comfort so that the comparison is not driven by performance differences. Describe exactly what differs (textures, lighting, props, audio) so that "realism" is not a bundle of unreported changes.

**Measures.** Knowledge test (pre, post, delayed), transfer task, time, errors, critical misses, presence (Schubert et al., 2001), NASA-TLX, SSQ, SUS.

**Limitations.** Realism is multi-dimensional; one low and one high setting cannot isolate which element matters. Participants may notice and react to the manipulation. Hardware-dependent frame rate differences can confound results. Cybersickness could differ between conditions.

## Study C: platform comparison (desktop, Vive, Quest)

**Maps to.** The same scenario on a desktop browser, the HTC Vive Pro 2 with SteamVR, and the Meta Quest (Quest Browser or standalone). The multi-platform builds exist for several examples, but cross-platform parity of each scenario would need to be checked and documented first; no cross-platform equivalence is claimed here.

**Research questions.**
1. Do knowledge, transfer, and time differ across desktop, tethered/wireless PC VR, and standalone VR?
2. How do comfort (SSQ), presence, usability, and workload differ?

**Hypotheses.**
- H1: Presence will be higher in the headset conditions than on desktop.
- H2: Knowledge and transfer scores will be similar across platforms (consistent with the reviews' general finding of rough parity, Hamilton et al., 2021; and with Buttussi & Chittaro, 2018, who reported learning gains regardless of display), with headset conditions showing more symptoms on the SSQ.
- H3: Differences between the two headsets will be small for learning; any difference in tracking or comfort is an empirical question.

**Conditions.** Desktop (mouse and keyboard or equivalent), Vive Pro 2 with controllers, Quest with controllers. Optionally add hand tracking versus controllers on the Quest; the verified evidence so far found no clear learning difference between them (Khundam et al., 2021; Saran, 2025), so this would be exploratory.

**Measures.** Knowledge test (pre, post, delayed), transfer task (preferably the same physical or desktop-neutral task for all platforms), time, errors, critical misses, SSQ, presence, SUS, NASA-TLX.

**Limitations.** Headset sessions are logistically costly with one or two devices, which limits throughput. Platform differences are bundled (input method, field of view, resolution, frame rate, setup effort), so a difference cannot be assigned to one cause. Novelty effects with headsets. Desktop participants have no cybersickness exposure, so SSQ comparisons are limited. The apps have not been tested on real headsets by the agents that built them; any hardware claim must come from testing by the research team.

## Order of work (suggested)

1. Read the full texts of the cited sources and decide the main outcome measure for the first study.
2. Pilot the knowledge test and transfer task with a few colleagues (not as research data) to check clarity and timing.
3. Submit the protocol to the institution's review process before recruiting.
4. Decide and build the logging and any comparison builds.
5. Run a power analysis, then collect data.

## References used in this document

All are listed in [references.bib](references.bib): Kaplan et al. (2021); Johnson-Glenberg (2018); Slater (2009); Makransky et al. (2019); Parong & Mayer (2018); Hamilton et al. (2021); Scorgie et al. (2024); Stefan et al. (2023); Poyade et al. (2021); Moro et al. (2017); Buttussi & Chittaro (2018); Khundam et al. (2021); Saran (2025); Sweller (1988); Mayer & Moreno (2003); Kennedy et al. (1993); Rebenitsch & Owen (2016); Schubert et al. (2001); Brooke (1996); Hart & Staveland (1988).
