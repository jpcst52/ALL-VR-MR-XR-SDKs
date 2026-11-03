# Citation verification (round 2, 2026-11-03)

An independent re-check of every entry in [references.bib](references.bib) and every research claim in this folder and in the repository README. The first pass of this bibliography was made on 2026-11-01; this review did not rely on it.

## Method

- **Bibliographic record.** Each DOI was queried at the Crossref REST API (`api.crossref.org/works/<doi>`) and compared with the `.bib` entry on title, author count and first author, year, venue, volume, issue and pages. The Purdue LOTO package is a DataCite DOI and was checked at `api.datacite.org`. The Rocketbox GitHub licence was read from the GitHub API.
- **Abstract.** Where a claim in a document is more than "this paper exists", the abstract was read (Crossref, OpenAlex, Semantic Scholar, PubMed or a publisher or repository page) and compared with the wording used here. Only abstracts were read, never full texts.
- **Not done.** No full texts. Author affiliations and the "Institution" columns of the landscape file were not re-derived. Web pages for labs and programs (section 7 of the landscape file) and the OEXR and NC State sites were not re-opened.
- The machine comparison treated these as benign: diacritics in first-author names, an HTML `&amp;` in a Crossref title, Crossref listing an organisation row beside a person, and DataCite DOIs absent from Crossref.

## Result

| Measure | Count |
|---|---|
| Entries in `references.bib` after review | 77 (76 original plus Rocketbox) |
| Record verified against Crossref (title, authors, year, venue, volume, pages, DOI) | 76 |
| Record verified against DataCite (not Crossref) | 1 (Akdere et al., LOTO) |
| Entries whose bibliographic record could not be verified | 0 |
| Entries with no abstract in any index (record verified, findings **unverifiable**) | 10 |
| Corrections to the `.bib` | 10 (list below) |
| Prose claims corrected or tightened | 16 (table below) |

### Fixes to `references.bib`

1. `muda2024advancing`: author "Muda and others" replaced with the four authors in Crossref (Muda, Misnan, Sam, Abdul Aziz); volume 16, issue 9 and DOI 10.30880/ijie.2024.16.09.012 added; the "author list incomplete" note removed.
2. DOIs added where the entry had none and Crossref returned an exact match on title, authors, year, volume and pages: `radianti2020systematic`, `makransky2019adding`, `sacks2013construction`, `kennedy1993simulator`, `rebenitsch2016review`.
3. `radianti2020systematic`: article number 103778 added.
4. `michalski2019using`: article number 2159 added (was "to confirm").
5. `wehrmann2025systematic`: article number e70112 added (was "not read").
6. `heibel2023virtual`: note updated (Crossref lists no page range).
7. `poupard2025systematic`: note clarified (online 2024-07-16, issue 2025-01).
8. `scorgie2024virtual`: note updated (Crossref gives initials only).
9. New entry `gonzalezfranco2020rocketbox` (Microsoft Rocketbox paper, see below).
10. File header rewritten to state the 2026-11-03 re-check.

### Microsoft Rocketbox

Gonzalez-Franco, M., et al. (2020). *The Rocketbox Library and the Utility of Freely Available Rigged Avatars.* Frontiers in Virtual Reality, 1, 561558. DOI 10.3389/frvir.2020.561558. Crossref: 17 authors, published online 2020-11-03, CC BY 4.0. It was **not** in the bib before this review. It is now entry `gonzalezfranco2020rocketbox` and has its own section 4c in [README.md](README.md). The library itself is MIT licensed (GitHub API for `microsoft/Microsoft-Rocketbox` reports MIT). The documents already say it is used for the local hub people: `vive-steamvr/hub/README.md` (section "People (Microsoft Rocketbox, local only)", cites the DOI), `docs/GLOSSARY.md` ("licensed local assets") and `docs/PROJECT-STATUS.md`. The files stay local and are not committed. Not verified here: whether the downloaded avatar files carry the same licence text, because the local `Assets/Rocketbox` folder was not inspected (the Unity editor is open on the hub).

## Claims that overreached the source

| Where | Original wording | Problem | Corrected wording |
|---|---|---|---|
| README 1 (Kaplan et al.) and root README | "likely advantage where danger or cost makes conventional training hard to run"; "strongest case where real practice is hazardous" | The abstract says XR is as effective as accepted methods and that its *value lies in* circumstances that exclude traditional methods (danger, cost). It reports no advantage. | "about as effective ... the authors conclude its value lies in circumstances that exclude traditional methods"; root README: "with its value lying where danger or cost make traditional training impossible" |
| README 2 (Sacks et al.) | "the immersive group did better at identifying hazards (as reported in the abstract and in later citing work)" | The abstract summary read says VR beat classroom training for specific trades (stone cladding, cast-in-situ concrete) but **not** for general site safety; it does not mention hazard identification. "Later citing work" was not a source. | Rewritten to the trade-specific result: 66 participants, tested before, immediately after and at one month |
| README 2 (Muda et al.) | "Muda et al." | Four named authors, not an open list | Full author list |
| README 3 (Awori et al.) | "Single-centre study ... authors note small sample" | Neither statement is in the abstract | Removed; now states n = 30, ratings 8.5 vs 6.3 vs 5.5, 87% preferred VR, and that it is perception data |
| README 1b (Pellas et al. 2020) | "41 studies" from "a search-result summary ... to confirm" | Now confirmed in the abstract (41 studies, 2009 to 2019) | Confirmed, "to confirm" removed |
| README 1b (Poupard et al.) | Extraneous-load finding "from a secondary summary and is to confirm" | Confirmed in the abstract (36 studies; VR often causes extraneous load, hindering novices) | Confirmed, with the AR contrast |
| README 1b (Buttussi and Chittaro) | Finding "seen through search ... should be confirmed" | Confirmed in the abstract | Confirmed |
| README 2d (Michalski et al.) | counts "left out and to confirm" | Abstract: 448 articles, 4 eligible | Counts added |
| README 2e (Wehrmann and Zender) | "search summary ... unconfirmed (abstract not read)" | Abstract read: 89 studies, collaborative 44%, cooperative 37%, cooperative VR "underexplored" | Replaced with the abstract content |
| README 4b (Khundam et al.) | "45 students (per a search summary; to confirm)" | Confirmed in the abstract | Confirmed |
| README 2 gaps | "Evidence on VR lockout/tagout training was mostly vendor case studies and small or preliminary studies" | No cited source supports it | Marked **unverified**; do not state in a paper without a fresh search |
| README 3 (Maresky et al.) | "randomized pilot ... viable and effective" | Not wrong, but no numbers | 42 students (14 control, 28 VR), 23.9% higher overall quiz score in the abstract |
| STUDY-DESIGNS (Study C, H2) | "the reviews' general finding of rough parity, Hamilton et al., 2021" | Hamilton et al. report that most studies found a significant advantage for immersive VR, a smaller number no difference, two clear negatives. Wu et al. find a small HMD advantage (ES 0.24). Neither is "parity". | Rewritten: Buttussi reported gains regardless of display; the reviews show mostly small advantages or no difference; "similar" is a hypothesis |
| Landscape (Wish-Baratz et al.) | "Results were not read" | Abstract read | Results added (185 students, 177 responded, 81% equivalent or better; single institution) |
| Landscape (Gonzalez et al.; Thees et al. 2021) | "abstract truncated" / "results not read" | Full abstracts read | Results added |
| Landscape (Tsirulnikov et al.) | "Mean scores rose from 64.1% to 82.3%" | Not in the abstract; the PMC page was blocked by a reCAPTCHA on 2026-11-03 | Abstract-level result stated; the percentages flagged unverified |

## Confirmed without change (the load-bearing claims)

Checked against the abstract and found accurate: Slater (2009) two illusions; Johnson-Glenberg (2018) two affordances; Makransky et al. (2019) 52 students, presence d = 1.30, learning lower d = 0.80, EEG load d = 0.59; Radianti et al. (2020) theories rarely used and evaluation mostly usability; Hamilton et al. (2021) 29 studies; Scorgie et al. (2024) 52 articles, 9.6% theory, 36% retention, two meta-analyses favour VR; Stefan et al. (2023) 136 studies, 72.06% learning and 66.18% reaction levels; Poyade et al. (2021) equivalence; Stone et al. (2011) 41.6% more certifications; Wang and Messner (2026) 60 students, 27%, 28%, 45%, and neither method conveyed the rationale; Man et al. (2024) 0.593, 0.432, 0.777; Nykänen et al. (2020) 119 workers, 8 workplaces; Jeelani et al. (2020) 39% and 44%, 53 participants; Kim et al. (2021, 2023); Shi et al. (2019) n = 126; Eiris et al. (2020) inverse correlation of presence and hazard score; Choi et al. (2020); Abbas et al. (2023); Luo et al. (2023) 120 participants 2 x 2; Moro et al. (2017) and (2021) numbers; Zhao et al. (2020) SMD 0.53, I-squared 87.8%; Bölek et al. (2021) 5 studies, 508 participants; Wu et al. (2020) ES 0.24; Caserman et al. (2021) 49 publications; Andersen and Makransky (2021) studies of 73, 140, 121.

## Still unverifiable (flagged, not removed)

These have a verified bibliographic record but no abstract in Crossref, OpenAlex, Semantic Scholar or PubMed, so any description of their findings rests on a repository or vendor page. The documents already label them; do not quote findings without reading the paper.

- `pellas2021immersive` (no description of findings is given)
- `manca2013bridging`, `ko2021development` (no findings claimed)
- `patle2019operator` (summary from the Politecnico di Milano repository)
- `coban2022potential` (48 studies, g = 0.38, from the Atatürk University repository)
- `makransky2017development` (sample sizes from a Labster page, a vendor)
- `kennedy1993simulator`, `rebenitsch2016review`, `hart1988development`, `brooke1996sus` (instrument sources; record only)
- Landscape rows that depend on web pages not re-opened on 2026-11-03: the Mayrose NIOSH report, OEXR, NC State XR, and the lab and program table in section 7.
- Crossref lists no author for `brooke1996sus` and `hart1988development`; authorship comes from secondary indexes and was not re-checked.
- The "Institution(s)" column in the landscape file was not re-derived and may reflect affiliations at a different date.
- `bolek2021effectiveness`: three authors in Crossref, four in OpenAlex; unresolved.
- `tsirulnikov2023game`: the 64.1% to 82.3% scores.

## Other documents that cite research

- Root `README.md` (one paragraph citing Kaplan, Johnson-Glenberg, Makransky 2019): the Kaplan clause was corrected; the other two clauses match their abstracts.
- `docs/research/STUDY-DESIGNS.md`: every claim attributed to a source was checked against the abstract. One correction (Study C, H2). The statements about Wang and Messner (2026) are accurate.
- `training-simulations/reasons.js` header cites Wang and Messner (2026) in the same way; that is accurate.
- `vive-steamvr/hub/README.md` was read only (the Rocketbox citation is correct); it was not edited.

## Per-entry table

"Verified against Crossref" means title, author count, first author, year (print or issue year), venue, volume, issue and pages match the `.bib` entry (after the fixes above). "Abstract" says what was read for the claims.

| # | Key | Citation | DOI | Record | Abstract | Notes |
|---|---|---|---|---|---|---|
| 1 | `kaplan2021effects` | Kaplan et al. (2021) | 10.1177/0018720820904229 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Claim corrected in README: value lies where traditional training is impossible, not a general advantage. |
| 2 | `radianti2020systematic` | Radianti et al. (2020) | 10.1016/j.compedu.2019.103778 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | DOI added (was missing). Claim matches abstract. |
| 3 | `makransky2019adding` | Makransky et al. (2019) | 10.1016/j.learninstruc.2017.12.007 | Verified against Crossref | Abstract read (Semantic Scholar) | DOI added (was missing). Claim (52 students, presence up, learning down, EEG load up) matches abstract. |
| 4 | `johnsonglenberg2018immersive` | Johnson-Glenberg (2018) | 10.3389/frobt.2018.00081 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 5 | `slater2009place` | Slater (2009) | 10.1098/rstb.2009.0138 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 6 | `sacks2013construction` | Sacks et al. (2013) | 10.1080/01446193.2013.828844 | Verified against Crossref | Abstract read (RePEc summary only) | DOI added. Claim corrected: VR beat classroom training for specific trades, not general safety; "identifying hazards" removed. |
| 7 | `muda2024advancing` | Muda et al. (2024) | 10.30880/ijie.2024.16.09.012 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Authors corrected (was "Muda and others"); volume 16(9) and DOI added. |
| 8 | `maresky2019virtual` | Maresky et al. (2019) | 10.1002/ca.23292 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Abstract read; numbers added. |
| 9 | `kennedy1993simulator` | Kennedy et al. (1993) | 10.1207/s15327108ijap0303_3 | Verified against Crossref | No abstract available: bibliographic record only | DOI added. |
| 10 | `schubert2001experience` | Schubert et al. (2001) | 10.1162/105474601300343603 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 11 | `rebenitsch2016review` | Rebenitsch et al. (2016) | 10.1007/s10055-016-0285-9 | Verified against Crossref | No abstract available: bibliographic record only | DOI added. Crossref/S2 have no abstract. |
| 12 | `sweller1988cognitive` | Sweller (1988) | 10.1207/s15516709cog1202_4 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 13 | `mayer2003nine` | Mayer et al. (2003) | 10.1207/s15326985ep3801_6 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 14 | `sweller2019cognitive` | Sweller et al. (2019) | 10.1007/s10648-019-09465-5 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 15 | `poupard2025systematic` | Poupard et al. (2025) | 10.1111/bjet.13503 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Crossref: online 2024-07, issue 2025-01 (56:1). Claim about extraneous load for novices now confirmed in abstract. |
| 16 | `parong2018learning` | Parong et al. (2018) | 10.1037/edu0000241 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Abstract read; README now states Experiment 1 result. |
| 17 | `hamilton2021immersive` | Hamilton et al. (2021) | 10.1007/s40692-020-00169-2 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 18 | `pellas2020scoping` | Pellas et al. (2020) | 10.1109/TLT.2020.3019405 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Abstract read: 41 studies, 2009-2019 now confirmed. |
| 19 | `pellas2021immersive` | Pellas et al. (2021) | 10.1007/s10055-020-00489-9 | Verified against Crossref | No abstract available: bibliographic record only | No abstract available; background citation only. |
| 20 | `buttussi2018effects` | Buttussi et al. (2018) | 10.1109/TVCG.2017.2653117 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 21 | `michalski2019using` | Michalski et al. (2019) | 10.3389/fpsyg.2019.02159 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Article number 2159 added; 448/4 counts confirmed in abstract. |
| 22 | `stefan2023evaluating` | Stefan et al. (2023) | 10.1007/s10055-023-00843-7 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 23 | `scorgie2024virtual` | Scorgie et al. (2024) | 10.1016/j.ssci.2023.106372 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Initials only in Crossref; claim confirmed. |
| 24 | `poyade2021transferable` | Poyade et al. (2021) | 10.1021/acs.chas.0c00105 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 25 | `stone2011physical` | Stone et al. (2011) | 10.1177/0018720811413389 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 26 | `heibel2023virtual` | Heibel et al. (2023) | 10.5032/jae.v64i4.38 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Crossref lists no pages. Abstract: 18 articles, 2012-2022. |
| 27 | `vandermeer2023virtual` | van der Meer et al. (2023) | 10.3389/frvir.2023.1159905 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 28 | `wehrmann2025systematic` | Wehrmann et al. (2025) | 10.1111/jcal.70112 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Article number e70112 added. Abstract read. |
| 29 | `dunmoye2025systematic` | Dunmoye et al. (2025) | 10.1007/s10055-025-01280-4 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 30 | `moro2017effectiveness` | Moro et al. (2017) | 10.1002/ase.1696 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 31 | `awori2023comparative` | Awori et al. (2023) | 10.1186/s41205-022-00164-6 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Over-reading removed (single-centre, authors note small sample). |
| 32 | `rochaestrada2022evaluation` | Rocha Estrada et al. (2022) | 10.3389/feduc.2022.918125 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 33 | `khundam2021no` | Khundam et al. (2021) | 10.3352/jeehp.2021.18.22 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | n = 45 now confirmed in abstract. |
| 34 | `saran2025comparing` | Saran (2025) | 10.7717/peerj-cs.3168 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 35 | `brooke1996sus` | Brooke (1996) | 10.1201/9781498710411-35 | Verified against Crossref | No abstract available: bibliographic record only | Crossref has no author field (author from a UCL record, not re-checked). Pages 207-212 in Crossref vs 189-194 in common citations: unresolved. |
| 36 | `hart1988development` | Hart et al. (1988) | 10.1016/S0166-4115(08)62386-9 | Verified against Crossref | No abstract available: bibliographic record only | Crossref has no author field; authorship from secondary indexes, not re-checked. No abstract. |
| 37 | `jeelani2020development` | Jeelani et al. (2020) | 10.1108/ECAM-07-2019-0391 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 38 | `eiris2020desktop` | Eiris et al. (2020) | 10.1016/j.autcon.2019.102969 | Verified against Crossref | Abstract read (Semantic Scholar) |  |
| 39 | `shi2019impact` | Shi et al. (2019) | 10.1016/j.autcon.2019.04.015 | Verified against Crossref | Abstract read (Semantic Scholar) |  |
| 40 | `kim2021reducing` | Kim et al. (2021) | 10.1061/(ASCE)CO.1943-7862.0002187 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 41 | `kim2023virtual` | Kim et al. (2023) | 10.1016/j.isci.2022.105827 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Crossref 2023 (26:1); online 2022 per OpenAlex. |
| 42 | `nykanen2020implementing` | Nyk\anen et al. (2020) | 10.1016/j.jsr.2020.09.015 | Verified against Crossref | Abstract read (PubMed) | Title/authors OK (first-author diacritic only). PubMed abstract confirms outcomes. |
| 43 | `man2024virtual` | Man et al. (2024) | 10.1016/j.jsr.2023.11.011 | Verified against Crossref | Abstract read (PolyU portal/Semantic Scholar) |  |
| 44 | `wang2026enhancing` | Wang et al. (2026) | 10.36680/j.itcon.2026.023 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Crossref page = 507 (start); 507-537 from ITcon page. Full abstract read; the rationale claim is confirmed. |
| 45 | `renganayagalu2021effectiveness` | Renganayagalu et al. (2021) | 10.1007/s10758-020-09489-9 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 46 | `choi2020vrbased` | Choi et al. (2020) | 10.1016/j.aap.2019.105404 | Verified against Crossref | Abstract read (PubMed/Semantic Scholar) |  |
| 47 | `abbas2023how` | Abbas et al. (2023) | 10.1061/(ASCE)ME.1943-5479.0001101 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 48 | `luo2023investigating` | Luo et al. (2023) | 10.1016/j.dibe.2023.100223 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 49 | `patle2019operator` | Patle et al. (2019) | 10.1007/s10055-018-0354-3 | Verified against Crossref | No abstract available: bibliographic record only | No abstract in any index; summary from a university repository: unverified. |
| 50 | `manca2013bridging` | Manca et al. (2013) | 10.1016/j.advengsoft.2012.09.002 | Verified against Crossref | No abstract available: bibliographic record only | No abstract available; no findings claimed. |
| 51 | `ko2021development` | Ko et al. (2021) | 10.1007/s11814-021-0804-6 | Verified against Crossref | No abstract available: bibliographic record only | No abstract available; no findings claimed. |
| 52 | `vercelli2025sampai` | Vercelli et al. (2025) | 10.1109/IEEE-CH65308.2025.11279275 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Abstract confirms a planned (not reported) validation. Affiliation unverified. |
| 53 | `altmeyer2020use` | Altmeyer et al. (2020) | 10.1111/bjet.12900 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 54 | `thees2022augmented` | Thees et al. (2022) | 10.3389/fpsyg.2022.804742 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 55 | `thees2021comparing` | Thees et al. (2021) | 10.3389/feduc.2021.705551 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Full abstract read; results added. |
| 56 | `wolbach2024leveraging` | Wolbach et al. (2024) | 10.3390/virtualworlds3030016 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 57 | `tsirulnikov2023game` | Tsirulnikov et al. (2023) | 10.1002/2211-5463.13567 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Crossref title uses an HTML ampersand only. PMC page unreachable: "64.1% to 82.3%" scores unverified. |
| 58 | `moro2021virtual` | Moro et al. (2021) | 10.1002/ase.2049 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 59 | `zhao2020effectiveness` | Zhao et al. (2020) | 10.1186/s12909-020-1994-z | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 60 | `bolek2021effectiveness` | B\olek et al. (2021) | 10.1038/s41598-021-94721-4 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Crossref lists 3 authors; OpenAlex a 4th (unresolved). |
| 61 | `zinchenko2020virtual` | Zinchenko et al. (2020) | 10.1016/j.newideapsych.2020.100786 | Verified against Crossref | Abstract read (Semantic Scholar) |  |
| 62 | `gonzalez2020augmented` | Gonzalez et al. (2020) | 10.1152/advan.00137.2019 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Full abstract read; final-exam result added. |
| 63 | `wishbaratz2020assessment` | Wish-Baratz et al. (2020) | 10.1001/jamanetworkopen.2020.16271 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Full abstract read; results added. |
| 64 | `baratz2022comparing` | Baratz et al. (2022) | 10.5116/ijme.6250.0af8 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 65 | `han2023people` | Han et al. (2023) | 10.1093/jcmc/zmac031 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 66 | `huisinga2023adding` | Huisinga (2023) | 10.3390/virtualworlds2030014 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 67 | `jarusevicius2024transforming` | Jaru\v sevi\v cius et al. (2024) | 10.3390/app14146366 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | OK (diacritic in first author only). |
| 68 | `krokos2019virtual` | Krokos et al. (2019) | 10.1007/s10055-018-0346-3 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 69 | `wu2020effectiveness` | Wu et al. (2020) | 10.1111/bjet.13023 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 70 | `coban2022potential` | Coban et al. (2022) | 10.1016/j.edurev.2022.100452 | Verified against Crossref | No abstract available: bibliographic record only | No abstract in any index; the 48-studies/g=0.38 summary rests on a university repository page: unverified. |
| 71 | `makransky2021camil` | Makransky et al. (2021) | 10.1007/s10648-020-09586-2 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 72 | `makransky2017development` | Makransky et al. (2017) | 10.1016/j.chb.2017.02.066 | Verified against Crossref | No abstract available: bibliographic record only | No abstract in any index; 161/118 student samples come from a vendor page: unverified. |
| 73 | `andersen2021validation` | Andersen et al. (2021) | 10.1111/jcal.12478 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 74 | `caserman2021cybersickness` | Caserman et al. (2021) | 10.1007/s10055-021-00513-6 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 75 | `abeywardena2023oxref` | Abeywardena (2023) | 10.19173/irrodl.v24i3.7109 | Verified against Crossref | Abstract read (Crossref/OpenAlex) |  |
| 76 | `akdere2020loto` | Akdere et al. (2020) | 10.4231/W9SK-NZ50 | Verified (DataCite record) | Description read (DataCite) | Checked in DataCite (not Crossref): 7 creators, 2020, v1.0, CC BY-NC 4.0. Description has no evaluation. |
| 77 | `gonzalezfranco2020rocketbox` | Gonzalez-Franco et al. (2020) | 10.3389/frvir.2020.561558 | Verified against Crossref | Abstract read (Crossref/OpenAlex) | Added in this review (was missing). 17 authors, Frontiers in Virtual Reality 1:561558, 2020-11-03. |
