# Research background

This folder connects the examples in this repository to the published research on immersive training, and lists where the evidence is thin and a study here could contribute. It is written to support future papers, grant conversations, and advisory-board discussions. It is a working bibliography, not a literature review.

**How the references were checked.** Each entry below was confirmed against at least one bibliographic source (publisher, DOI, library, or index page). Summaries describe only what those sources showed, which was usually the abstract. Before citing any of them in a paper, read the full text, and add or confirm the DOI where one is not shown here. [`references.bib`](references.bib) holds the same entries for LaTeX or Zotero.

## 1. What the evidence says about immersive training

| Reference | What it shows | Why it matters for these examples |
|---|---|---|
| Kaplan, A. D., Cruit, J., Endsley, M., Beers, S. M., Sawyer, B. D., & Hancock, P. A. (2021). The effects of virtual reality, augmented reality, and mixed reality as training enhancement methods: A meta-analysis. *Human Factors, 63*(4), 706–726. [doi:10.1177/0018720820904229](https://doi.org/10.1177/0018720820904229) | A meta-analysis finding that XR training performs about as well as commonly used training methods overall, with a likely advantage where danger or cost makes conventional training hard to run. | Supports the premise of the industrial scenarios (hot work, electrical isolation, pressure testing): the case for VR is strongest where the real task is hazardous. It also means "as good as" is the realistic expectation, so studies should be designed to detect that. |
| Radianti, J., Majchrzak, T. A., Fromm, J., & Wohlgenannt, I. (2020). A systematic review of immersive virtual reality applications for higher education: Design elements, lessons learned, and research agenda. *Computers & Education, 147*. | A systematic mapping of immersive VR in higher education. It reports that learning theories were often not used to guide design and that evaluations mostly measured usability rather than learning outcomes. | A direct research opportunity: these examples are built around explicit mechanics (ordered steps, consequences, scoring), so they can be evaluated for learning outcomes, not just usability. |
| Makransky, G., Terkildsen, T. S., & Mayer, R. E. (2019). Adding immersive virtual reality to a science lab simulation causes more presence but less learning. *Learning and Instruction, 60*, 225–236. | In a study of 52 university students, immersive VR increased presence but produced lower learning scores and higher EEG-measured cognitive load than the same simulation on a desktop. | A caution about realism. More immersion is not automatically better learning, so the realism work in this project should be tested against simpler versions, and extraneous visual detail should be kept from competing with the task. |
| Johnson-Glenberg, M. C. (2018). Immersive VR and education: Embodied design principles that include gesture and hand controls. *Frontiers in Robotics and AI, 5*, 81. [doi:10.3389/frobt.2018.00081](https://doi.org/10.3389/frobt.2018.00081) | A theory-based paper arguing that presence and hand-based manipulation are VR's distinctive educational affordances, and proposing embodied design principles. | The conceptual basis for the hands-on redesign: tasks are carried out with the hands and controller (grab, turn, point) rather than chosen from a list. |
| Slater, M. (2009). Place illusion and plausibility can lead to realistic behaviour in immersive virtual environments. *Philosophical Transactions of the Royal Society B, 364*(1535), 3549–3557. [doi:10.1098/rstb.2009.0138](https://doi.org/10.1098/rstb.2009.0138) | Proposes two illusions that together drive realistic responses in VR: place illusion (feeling of being there) and plausibility illusion (feeling that events are really happening). | A framework for the scenarios' design. Consequences that respond to the learner's actions (a relief valve lifting, a flashing critical-miss warning) target plausibility more than visual realism does. |

## 2. Safety and industrial training

| Reference | What it shows | Why it matters |
|---|---|---|
| Sacks, R., Perlman, A., & Barak, R. (2013). Construction safety training using immersive virtual reality. *Construction Management and Economics, 31*(9), 1005–1017. [Listing](https://ideas.repec.org/a/taf/conmgt/v31y2013i9p1005-1017.html) | Compared hazard-recognition training in an immersive virtual site with conventional 2D training; the immersive group did better at identifying hazards (as reported in the abstract and in later citing work). | An early, frequently cited result for hazard-recognition training, and a template for a comparison study here (hands-on 3D task versus the earlier choose-a-response format). |
| Muda et al. (2024). Advancing safety in the oil and gas infrastructure via virtual reality technology: A comprehensive exploration of hazard simulation and visualisations. *International Journal of Integrated Engineering.* [Article](https://publisher.uthm.edu.my/ojs/index.php/ijie/article/view/17161) | A review of VR research in oil and gas that covers simulation of hazards such as gas leaks, fires, and equipment failure. | Context for the refinery battery-limits and hydrostatic-test scenarios. |

**Gaps found while searching (as of this writing).** These are good candidates for original work, but confirm with a fuller literature search before claiming novelty.

- I did not find a systematic review measuring learning outcomes or incident reduction for VR training specifically in process industries (refining, chemical processing). The reviews found concentrated on hazard simulation or described applications.
- Evidence on VR lockout/tagout training was mostly vendor case studies and small or preliminary studies. A controlled study with a knowledge test and a hands-on transfer test would add to the literature.
- Evidence for browser-based WebXR specifically was mostly small pilots and perception surveys. I found no large controlled comparison of learning outcomes for WebXR versus native applications, which is relevant because most examples here are WebXR.

## 3. Anatomy and biology education

| Reference | What it shows | Why it matters |
|---|---|---|
| Maresky, H. S., Oikonomou, A., Ali, I., Ditkofsky, N., Pakkal, M., & Ballyk, B. (2019). Virtual reality and cardiac anatomy: Exploring immersive three-dimensional cardiac imaging, a pilot study in undergraduate medical anatomy education. *Clinical Anatomy, 32*(2), 238–243. [doi:10.1002/ca.23292](https://doi.org/10.1002/ca.23292) | A randomized pilot in which medical students used an anatomically accurate VR heart before learning cardiac anatomy; the authors conclude VR is viable and effective for this content. | Relevant to a proposed circulation-builder example for Human Anatomy and Physiology lab courses. It is a pilot, so effect sizes should not be over-read. |

## 4. Measurement instruments for studies

If a study is run, these are standard, citable instruments.

| Instrument | Reference |
|---|---|
| Simulator Sickness Questionnaire (SSQ), for comfort and cybersickness | Kennedy, R. S., Lane, N. E., Berbaum, K. S., & Lilienthal, M. G. (1993). Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness. *International Journal of Aviation Psychology, 3*(3), 203–220. |
| Presence (spatial presence, involvement, realness) | Schubert, T., Friedmann, F., & Regenbrecht, H. (2001). The experience of presence: Factor analytic insights. *Presence: Teleoperators and Virtual Environments, 10*(3), 266–281. [doi:10.1162/105474601300343603](https://doi.org/10.1162/105474601300343603) |
| Cybersickness background and display factors | Rebenitsch, L., & Owen, C. (2016). Review on cybersickness in applications and visual displays. *Virtual Reality, 20*(2), 101–125. |

Other widely used measures (for example the System Usability Scale and NASA-TLX for workload) were not verified in this session; look up and cite the original papers before use.

## 5. Study ideas these examples are set up for

These are starting points to discuss with collaborators, not finished designs. Any research with human participants needs review by the institution's human-subjects (IRB) process before data is collected.

1. **Hands-on tasks versus choose-a-response.** The Practice Lab previously used a choose-a-response format and now uses hands-on tasks in a 3D scene, and both exist in this repository's history. A between- or within-subjects comparison could measure knowledge, transfer to a new scenario, time, mistakes, and workload.
2. **Does realism help or hurt?** The same scenario can be run at low and high visual fidelity (the examples have a quality setting). This tests the presence-versus-load concern raised by Makransky et al. (2019) in a procedural-safety setting.
3. **Critical-mistake feedback.** The examples distinguish ordinary mistakes from critical ones and tell the learner immediately. Test whether that immediacy changes retention or later behavior.
4. **Platform comparison.** The same content runs on the HTC Vive with SteamVR, the Quest, and a desktop fallback. Compare learning, comfort (SSQ), and presence across them.
5. **Local partner scenarios.** Scenarios shaped around regional manufacturers (heat-exchanger testing, lighter sampling, resistor selection) allow studies with local employers and students that are not available from generic training content.

## 6. How to cite this project

Add a `CITATION.cff` once the repository has a stable release and an agreed author list; the repository is public, so a Zenodo DOI can be minted from a GitHub release.
