# Research background

This folder connects the examples in this repository to the published research on immersive training, and lists where the evidence is thin and a study here could contribute. It is written to support future papers, grant conversations, and advisory-board discussions. It is a working bibliography, not a literature review.

**How the references were checked.** Each entry below was confirmed against at least one bibliographic source (publisher, DOI, library, or index page). Summaries describe only what those sources showed, which was usually the abstract. Before citing any of them in a paper, read the full text, and add or confirm the DOI where one is not shown here. [`references.bib`](references.bib) holds the same entries for LaTeX or Zotero.

**University and lab landscape.** [UNIVERSITY-VR-AR-LANDSCAPE.md](UNIVERSITY-VR-AR-LANDSCAPE.md) surveys VR/AR research and programs at other universities (industrial safety, forklift, process industry, circuits, cardiovascular, multi-user/WebXR, measurement, open material), with build-on ideas mapped to our scenarios and study designs, collaboration leads, and its own verification notes. Its 40 entries are appended to `references.bib`.

Entries added on 2026-11-01 were checked against Crossref records (title, authors, venue, volume, pages, DOI) or a publisher or PubMed Central page. Where a record had no abstract, the entry says so and no findings are claimed. Anything marked "to confirm" could not be verified and is left out of the citation until it is.

## 1. What the evidence says about immersive training

| Reference | What it shows | Why it matters for these examples |
|---|---|---|
| Kaplan, A. D., Cruit, J., Endsley, M., Beers, S. M., Sawyer, B. D., & Hancock, P. A. (2021). The effects of virtual reality, augmented reality, and mixed reality as training enhancement methods: A meta-analysis. *Human Factors, 63*(4), 706–726. [doi:10.1177/0018720820904229](https://doi.org/10.1177/0018720820904229) | A meta-analysis finding that XR training performs about as well as commonly used training methods overall, with a likely advantage where danger or cost makes conventional training hard to run. | Supports the premise of the industrial scenarios (hot work, electrical isolation, pressure testing): the case for VR is strongest where the real task is hazardous. It also means "as good as" is the realistic expectation, so studies should be designed to detect that. |
| Radianti, J., Majchrzak, T. A., Fromm, J., & Wohlgenannt, I. (2020). A systematic review of immersive virtual reality applications for higher education: Design elements, lessons learned, and research agenda. *Computers & Education, 147*. | A systematic mapping of immersive VR in higher education. It reports that learning theories were often not used to guide design and that evaluations mostly measured usability rather than learning outcomes. | A direct research opportunity: these examples are built around explicit mechanics (ordered steps, consequences, scoring), so they can be evaluated for learning outcomes, not just usability. |
| Makransky, G., Terkildsen, T. S., & Mayer, R. E. (2019). Adding immersive virtual reality to a science lab simulation causes more presence but less learning. *Learning and Instruction, 60*, 225–236. | In a study of 52 university students, immersive VR increased presence but produced lower learning scores and higher EEG-measured cognitive load than the same simulation on a desktop. | A caution about realism. More immersion is not automatically better learning, so the realism work in this project should be tested against simpler versions, and extraneous visual detail should be kept from competing with the task. |
| Johnson-Glenberg, M. C. (2018). Immersive VR and education: Embodied design principles that include gesture and hand controls. *Frontiers in Robotics and AI, 5*, 81. [doi:10.3389/frobt.2018.00081](https://doi.org/10.3389/frobt.2018.00081) | A theory-based paper arguing that presence and hand-based manipulation are VR's distinctive educational affordances, and proposing embodied design principles. | The conceptual basis for the hands-on redesign: tasks are carried out with the hands and controller (grab, turn, point) rather than chosen from a list. |
| Slater, M. (2009). Place illusion and plausibility can lead to realistic behaviour in immersive virtual environments. *Philosophical Transactions of the Royal Society B, 364*(1535), 3549–3557. [doi:10.1098/rstb.2009.0138](https://doi.org/10.1098/rstb.2009.0138) | Proposes two illusions that together drive realistic responses in VR: place illusion (feeling of being there) and plausibility illusion (feeling that events are really happening). | A framework for the scenarios' design. Consequences that respond to the learner's actions (a relief valve lifting, a flashing critical-miss warning) target plausibility more than visual realism does. |

### 1b. Reviews of immersive VR learning outcomes (added 2026-11-01)

| Reference | What it shows (abstract-level) | Why it matters |
|---|---|---|
| Hamilton, D., McKechnie, J., Edgerton, E., & Wilson, C. (2021). Immersive virtual reality as a pedagogical tool in education: A systematic literature review of quantitative learning outcomes and experimental design. *Journal of Computers in Education, 8*(1), 1–32. [doi:10.1007/s40692-020-00169-2](https://doi.org/10.1007/s40692-020-00169-2) | Reviewed 29 studies (since 2013) comparing head-mounted immersive VR with less immersive methods. Most found an advantage for immersive VR, a smaller number found no difference, two reported clear negative effects. Most interventions were short, few measured retention, and the authors judged learning assessment methods often inadequate. | A design checklist for this project: include a delayed retention test and a validated knowledge measure. |
| Pellas, N., Dengel, A., & Christopoulos, A. (2020). A scoping review of immersive virtual reality in STEM education. *IEEE Transactions on Learning Technologies, 13*(4), 748–761. [doi:10.1109/TLT.2020.3019405](https://doi.org/10.1109/TLT.2020.3019405) | Scoping review of immersive VR in STEM education. Crossref gave no abstract; the "41 studies, 2009 to 2019" scope comes from a search-result summary and is to confirm. | Context for engineering technology and STEM courses. Read the full text before using any detail. |
| Pellas, N., Mystakidis, S., & Kazanidis, I. (2021). Immersive virtual reality in K-12 and higher education: A systematic review of the last decade scientific literature. *Virtual Reality, 25*(3), 835–861. [doi:10.1007/s10055-020-00489-9](https://doi.org/10.1007/s10055-020-00489-9) | Systematic review; abstract not retrieved in this session. | Background citation only until the abstract is read. |
| Parong, J., & Mayer, R. E. (2018). Learning science in immersive virtual reality. *Journal of Educational Psychology, 110*(6), 785–797. [doi:10.1037/edu0000241](https://doi.org/10.1037/edu0000241) | Bibliographic record confirmed; abstract not retrieved in this session. | Companion to Makransky et al. (2019) on immersive VR for science learning; read the full text for its findings before citing them. |
| Buttussi, F., & Chittaro, L. (2018). Effects of different types of virtual reality display on presence and learning in a safety training scenario. *IEEE Transactions on Visualization and Computer Graphics, 24*(2), 1063–1076. [doi:10.1109/TVCG.2017.2653117](https://doi.org/10.1109/TVCG.2017.2653117) | Record confirmed on Crossref; the finding that display type affected engagement and presence while knowledge and self-efficacy gains appeared regardless of display, retained at two weeks, comes from a repository abstract seen through search and should be confirmed against the full text. | Directly relevant to the platform comparison (Study C) and to including a two-week follow-up. |

### 1c. Cognitive load and multimedia learning foundations (added 2026-11-01)

| Reference | What it shows | Why it matters |
|---|---|---|
| Sweller, J. (1988). Cognitive load during problem solving: Effects on learning. *Cognitive Science, 12*(2), 257–285. [doi:10.1207/s15516709cog1202_4](https://doi.org/10.1207/s15516709cog1202_4) | Argues that conventional problem solving can leave little processing capacity for schema acquisition (abstract-level summary). | Theoretical basis for treating extraneous detail as a cost. |
| Mayer, R. E., & Moreno, R. (2003). Nine ways to reduce cognitive load in multimedia learning. *Educational Psychologist, 38*(1), 43–52. [doi:10.1207/s15326985ep3801_6](https://doi.org/10.1207/s15326985ep3801_6) | Record confirmed (title, authors, volume, pages); abstract not retrieved. | Design-principles source for the on-screen text, labels, and narration in the scenarios. |
| Sweller, J., van Merriënboer, J. J. G., & Paas, F. (2019). Cognitive architecture and instructional design: 20 years later. *Educational Psychology Review, 31*(2), 261–292. [doi:10.1007/s10648-019-09465-5](https://doi.org/10.1007/s10648-019-09465-5) | Record confirmed; abstract not retrieved. | Current statement of cognitive load theory to cite alongside the 1988 paper. |
| Poupard, M., Larrue, F., Sauzéon, H., & Tricot, A. (2025). A systematic review of immersive technologies for education: Learning performance, cognitive load and intrinsic motivation. *British Journal of Educational Technology, 56*(1), 5–41. [doi:10.1111/bjet.13503](https://doi.org/10.1111/bjet.13503) | Recent review on learning performance, cognitive load and motivation in immersive technologies. Crossref dates it 2024 online; the statement in search summaries that VR often adds extraneous load for novices is from a secondary summary and is to confirm. | A VR-specific, recent review to pair with the foundations above. Confirm the year and findings from the full text. |

## 2. Safety and industrial training

| Reference | What it shows | Why it matters |
|---|---|---|
| Sacks, R., Perlman, A., & Barak, R. (2013). Construction safety training using immersive virtual reality. *Construction Management and Economics, 31*(9), 1005–1017. [Listing](https://ideas.repec.org/a/taf/conmgt/v31y2013i9p1005-1017.html) | Compared hazard-recognition training in an immersive virtual site with conventional 2D training; the immersive group did better at identifying hazards (as reported in the abstract and in later citing work). | An early, frequently cited result for hazard-recognition training, and a template for a comparison study here (hands-on 3D task versus the earlier choose-a-response format). |
| Muda et al. (2024). Advancing safety in the oil and gas infrastructure via virtual reality technology: A comprehensive exploration of hazard simulation and visualisations. *International Journal of Integrated Engineering.* [Article](https://publisher.uthm.edu.my/ojs/index.php/ijie/article/view/17161) | A review of VR research in oil and gas that covers simulation of hazards such as gas leaks, fires, and equipment failure. | Context for the refinery battery-limits and hydrostatic-test scenarios. |

### 2b. Safety-training reviews and process-industry studies (added 2026-11-01)

| Reference | What it shows (abstract-level) | Why it matters |
|---|---|---|
| Scorgie, D., Feng, Z., Paes, D., Parisi, F., Yiu, T. W., & Lovreglio, R. (2024). Virtual reality for safety training: A systematic literature review and meta-analysis. *Safety Science, 171*, 106372. [doi:10.1016/j.ssci.2023.106372](https://doi.org/10.1016/j.ssci.2023.106372) | Covers 52 articles (2013 to 2021) across 14 domains; two meta-analyses found VR safety training outperformed traditional training for knowledge acquisition and retention. Only 9.6% of studies explicitly adopted theories and only 36% measured long-term retention (per the repository abstract). | The strongest general support for the safety scenarios, and a direct pointer to the retention and theory gaps. |
| Stefan, H., Mortimer, M., & Horan, B. (2023). Evaluating the effectiveness of virtual reality for safety-relevant training: A systematic review. *Virtual Reality, 27*(4), 2839–2869. [doi:10.1007/s10055-023-00843-7](https://doi.org/10.1007/s10055-023-00843-7) | Reviewed 136 studies (2016 to August 2021). Using Kirkpatrick's four levels, most measured learning and reaction; very few measured behavior or results. | Supports adding a behavior-level transfer task, not just a knowledge test. |
| Poyade, M., Eaglesham, C., Trench, J., & Reid, M. (2021). A transferable psychological evaluation of virtual reality applied to safety training in chemical manufacturing. *ACS Chemical Health & Safety, 28*(1), 55–65. [doi:10.1021/acs.chas.0c00105](https://doi.org/10.1021/acs.chas.0c00105) | In an industrial case study, VR learning of complex safety tasks was statistically equivalent to slide-based video training, with gains in perceived learning and presence. Also notes retention from video may be overestimated if uncontrolled. | The closest process-industry precedent found. "Equivalent" is again the realistic expectation. |

### 2c. VR welding and hot-work training (added 2026-11-01)

| Reference | What it shows | Why it matters |
|---|---|---|
| Stone, R. T., Watts, K. P., Zhong, P., & Wei, C.-S. (2011). Physical and cognitive effects of virtual reality integrated training. *Human Factors, 53*(5), 558–572. [doi:10.1177/0018720811413389](https://doi.org/10.1177/0018720811413389) | Two randomly assigned 2-week welding courses with certified instructors. The VR-integrated group performed as well as the traditional group, outperformed it in some areas, and earned 41.6% more overall certifications (per the Crossref-listed abstract). | The most concrete welding comparison found. It is one study at one institution, and welding skill is not the same as hot-work safety awareness, which is what these examples cover. |
| Heibel, B., Anderson, R., & Drewery, M. (2023). Virtual reality in welding training and education: A literature review. *Journal of Agricultural Education, 64*(4). [doi:10.5032/jae.v64i4.38](https://doi.org/10.5032/jae.v64i4.38) | Literature review; abstract not read in this session, pages to confirm. | Starting point for the welding literature. |

### 2d. Transfer of training (added 2026-11-01)

| Reference | What it shows | Why it matters |
|---|---|---|
| Michalski, S. C., Szpak, A., & Loetscher, T. (2019). Using virtual environments to improve real-world motor skills in sports: A systematic review. *Frontiers in Psychology, 10*. [doi:10.3389/fpsyg.2019.02159](https://doi.org/10.3389/fpsyg.2019.02159) | Systematic review of real-world transfer from virtual environments (sport domain). Abstract details were seen only through search summaries (a small number of eligible studies), so counts are left out and to confirm. | Sport, not industry, but it frames transfer as under-tested. Kaplan et al. (2021) and Stefan et al. (2023) above are the closer transfer sources. |

### 2e. Collaborative and multi-user VR (added 2026-11-01)

| Reference | What it shows | Why it matters |
|---|---|---|
| van der Meer, N., van der Werf, V., Brinkman, W.-P., & Specht, M. (2023). Virtual reality and collaborative learning: A systematic literature review. *Frontiers in Virtual Reality, 4*, 1159905. [doi:10.3389/frvir.2023.1159905](https://doi.org/10.3389/frvir.2023.1159905) | PRISMA review of 139 articles. Authors report general optimism, five affordances (for example engagement, remote collaboration, social skills), and that most systems used monitor-based VR with mouse and keyboard. They conclude more research is needed on pedagogy. | Context for the multi-user WebXR examples (WaterWorks-style rooms, Circuit Lab, Site Safety Layout). |
| Wehrmann, F., & Zender, R. (2025). A systematic review of cooperation in multi-user virtual reality learning environments. *Journal of Computer Assisted Learning, 41*(5). [doi:10.1111/jcal.70112](https://doi.org/10.1111/jcal.70112) | Record confirmed on Crossref. A search summary described it as cataloguing multi-user applications and finding role-differentiated cooperative designs rarely studied; that description is unconfirmed (abstract not read). | Possible gap source for role-based cooperative tasks. Confirm before use. |
| Dunmoye, I. D., Smith, L. T., Brown, J. S., Martin, J. P., May, D., & Hunsu, N. (2025). A systematic review on the use of collaborative virtual reality in engineering education. *Virtual Reality, 30*(1), 31. [doi:10.1007/s10055-025-01280-4](https://doi.org/10.1007/s10055-025-01280-4) | Reviewed 27 empirical studies (2013 to 2023). Reports benefits for engagement, teamwork and problem solving, and barriers of cost, technical problems and preference for traditional methods. Calls for stronger theory and mixed methods. | Directly relevant to engineering technology courses at a small campus. |

**Gaps found while searching (as of 2026-11-01).** These are good candidates for original work, but confirm with a fuller literature search before claiming novelty. The searches were limited (a handful of queries, abstract-level reading) and do not amount to a systematic search.

- I did not find a systematic review measuring learning outcomes or incident reduction for VR training specifically in process industries (refining, chemical processing). The reviews found concentrated on hazard simulation or described applications. The 2021 chemical manufacturing case study (Poyade et al.) and the general safety-training reviews above (Scorgie et al.; Stefan et al.) exist, but none of those found is process-industry specific. Re-check, since this is a time-sensitive claim.
- Evidence on VR lockout/tagout training was mostly vendor case studies and small or preliminary studies. A controlled study with a knowledge test and a hands-on transfer test would add to the literature.
- Evidence for browser-based WebXR specifically was mostly small pilots and perception surveys (see section 4b). I found no large controlled comparison of learning outcomes for WebXR versus native applications, which is relevant because most examples here are WebXR.
- The safety-training reviews report that few studies measure long-term retention or on-the-job behavior (Scorgie et al.; Stefan et al.). A follow-up test and a transfer task would address this.
- Welding comparisons found were small, and I found nothing on VR for hot-work safety awareness (as distinct from welding skill) in this limited search.

## 3. Anatomy and biology education

| Reference | What it shows | Why it matters |
|---|---|---|
| Maresky, H. S., Oikonomou, A., Ali, I., Ditkofsky, N., Pakkal, M., & Ballyk, B. (2019). Virtual reality and cardiac anatomy: Exploring immersive three-dimensional cardiac imaging, a pilot study in undergraduate medical anatomy education. *Clinical Anatomy, 32*(2), 238–243. [doi:10.1002/ca.23292](https://doi.org/10.1002/ca.23292) | A randomized pilot in which medical students used an anatomically accurate VR heart before learning cardiac anatomy; the authors conclude VR is viable and effective for this content. | Relevant to a proposed circulation-builder example for Human Anatomy and Physiology lab courses. It is a pilot, so effect sizes should not be over-read. |
| Moro, C., Štromberga, Z., Raikos, A., & Stirling, A. (2017). The effectiveness of virtual and augmented reality in health sciences and medical anatomy. *Anatomical Sciences Education, 10*(6), 549–559. [doi:10.1002/ase.1696](https://doi.org/10.1002/ase.1696) | 59 students randomly assigned to learn skull anatomy with VR, AR, or a tablet app. Test scores did not differ significantly among groups; VR participants reported more adverse effects (headache, dizziness, blurred vision). | Another "as good as, not clearly better" result, plus a reminder to measure comfort (SSQ). |
| Awori, J., Friedman, S. D., Howard, C., Kronmal, R., & Buddhe, S. (2023). Comparative effectiveness of virtual reality (VR) vs 3D printed models of congenital heart disease in resident and nurse practitioner educational experience. *3D Printing in Medicine, 9*, 2. [doi:10.1186/s41205-022-00164-6](https://doi.org/10.1186/s41205-022-00164-6) | Single-centre study, n = 30; participants rated understanding higher with VR than with 3D printed models or traditional instruction, and most preferred VR. Self-reported, no objective post-test, authors note small sample. | Heart and circulation content, but perception data only; do not cite it as evidence of learning gains. |

Note: circulation-specific evidence beyond these was limited to small pilots and press-release-level reports in my search; nothing further is cited because it could not be verified against a bibliographic record.

## 4. Measurement instruments for studies

If a study is run, these are standard, citable instruments.

| Instrument | Reference |
|---|---|
| Simulator Sickness Questionnaire (SSQ), for comfort and cybersickness | Kennedy, R. S., Lane, N. E., Berbaum, K. S., & Lilienthal, M. G. (1993). Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness. *International Journal of Aviation Psychology, 3*(3), 203–220. |
| Presence (spatial presence, involvement, realness) | Schubert, T., Friedmann, F., & Regenbrecht, H. (2001). The experience of presence: Factor analytic insights. *Presence: Teleoperators and Virtual Environments, 10*(3), 266–281. [doi:10.1162/105474601300343603](https://doi.org/10.1162/105474601300343603) |
| Cybersickness background and display factors | Rebenitsch, L., & Owen, C. (2016). Review on cybersickness in applications and visual displays. *Virtual Reality, 20*(2), 101–125. |

Added 2026-11-01:

| Instrument | Reference |
|---|---|
| System Usability Scale (SUS) | Brooke, J. (1996). SUS: A "quick and dirty" usability scale. In *Usability Evaluation in Industry*. CRC Press. [doi:10.1201/9781498710411-35](https://doi.org/10.1201/9781498710411-35). Crossref confirms title, book, publisher and year but lists no author; the author comes from a UCL bibliography record. Editors to confirm; pages to confirm (Crossref lists 207–212 for the CRC edition, while 189–194 is the page range widely cited for the original print). |
| NASA Task Load Index (NASA-TLX), workload | Hart, S. G., & Staveland, L. E. (1988). Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research. In *Human Mental Workload* (Advances in Psychology, vol. 52, pp. 139–183). Elsevier/North-Holland. [doi:10.1016/S0166-4115(08)62386-9](https://doi.org/10.1016/S0166-4115(08)62386-9). Crossref confirms title, book, pages and DOI but lists no author; authorship comes from secondary indexes. Editors (reported as P. A. Hancock and N. Meshkati) to confirm. |

The original chapters, not later summaries, should be read before choosing a scoring variant (for example weighted versus unweighted NASA-TLX).

## 4b. WebXR, engineering/STEM, and hand-based interaction (added 2026-11-01)

| Reference | What it shows (abstract-level) | Why it matters |
|---|---|---|
| Rocha Estrada, F. J., Ruiz-Ramírez, J. A., George-Reyes, C. E., & Glasserman-Morales, L. D. (2022). Evaluation of a virtual campus adapted to web-based virtual reality spaces: Assessments of teachers and students. *Frontiers in Education, 7*, 918125. [doi:10.3389/feduc.2022.918125](https://doi.org/10.3389/feduc.2022.918125) | A technology-acceptance survey of a browser-based (WebVR) virtual campus. Participants generally accepted it; technical problems and connection quality were weaknesses. The authors suggest future studies examine learning outcomes with WebVR. | The only WebVR education study I could verify. It reports acceptance, not learning, which supports the gap noted in section 2. |
| Pellas et al. (2020) and Dunmoye et al. (2025), listed above | STEM and engineering reviews (see sections 1b and 2e). | Engineering technology context. |
| Khundam, C., Sukkriang, N., & Noël, F. (2021). No difference in learning outcomes and usability between using controllers and hand tracking during a virtual reality endotracheal intubation training for medical students in Thailand. *Journal of Educational Evaluation for Health Professions, 18*, 22. [doi:10.3352/jeehp.2021.18.22](https://doi.org/10.3352/jeehp.2021.18.22) | 45 students (per a search summary of the abstract; to confirm); both VR groups scored higher than a video-only group, and controller versus hand tracking did not differ significantly in outcomes or usability (SUS). | Suggests the input device may matter less for learning than the task design. One small study in one subject. |
| Saran, M. (2025). Comparing hand-based and controller-based interactions in virtual reality learning: Effects on presence and interaction performance. *PeerJ Computer Science, 11*, e3168. [doi:10.7717/peerj-cs.3168](https://doi.org/10.7717/peerj-cs.3168) | 54 sixth-grade students randomly assigned; no significant difference in presence or teacher-rated interaction performance between hand tracking and controllers in a virtual science lab. | Same direction as Khundam et al. A different study reporting higher presence with hand tracking was seen only in search results and is not cited. |

Johnson-Glenberg (2018) in section 1 remains the conceptual source for gesture and hand control; the empirical hand-tracking evidence found here is thin and does not show a learning advantage over controllers.

## 5. Study ideas these examples are set up for

Three of these are developed in [STUDY-DESIGNS.md](STUDY-DESIGNS.md) (hands-on versus choose-a-response, low versus high realism, and platform comparison). These are starting points to discuss with collaborators, not finished designs. Any research with human participants needs review by the institution's human-subjects (IRB) process before data is collected.

1. **Hands-on tasks versus choose-a-response.** The Practice Lab previously used a choose-a-response format and now uses hands-on tasks in a 3D scene, and both exist in this repository's history. A between- or within-subjects comparison could measure knowledge, transfer to a new scenario, time, mistakes, and workload.
2. **Does realism help or hurt?** The same scenario can be run at low and high visual fidelity (the examples have a quality setting). This tests the presence-versus-load concern raised by Makransky et al. (2019) in a procedural-safety setting.
3. **Critical-mistake feedback.** The examples distinguish ordinary mistakes from critical ones and tell the learner immediately. Test whether that immediacy changes retention or later behavior.
4. **Platform comparison.** The same content runs on the HTC Vive with SteamVR, the Quest, and a desktop fallback. Compare learning, comfort (SSQ), and presence across them.
5. **Local partner scenarios.** Scenarios shaped around regional manufacturers (heat-exchanger testing, lighter sampling, resistor selection) allow studies with local employers and students that are not available from generic training content.

## 6. How to cite this project

Add a `CITATION.cff` once the repository has a stable release and an agreed author list; the repository is public, so a Zenodo DOI can be minted from a GitHub release.
