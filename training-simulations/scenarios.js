export const scenarios = [
  {
    id: "customer-welcome",
    category: "business",
    title: "Welcome a walk-in",
    duration: "3 min",
    setting: "A neighborhood service counter",
    description: "Listen first, protect customer privacy, and agree on a useful next step.",
    object: "counter",
    steps: [
      {
        prompt: "A customer arrives while the counter is busy. What is your best opening?",
        choices: [
          { text: "Greet them, ask what they need, and give them your attention.", score: 2, feedback: "A welcoming, open question helps you understand the request without making assumptions." },
          { text: "Start explaining the newest offer before asking why they came.", score: 0, feedback: "A pitch can miss the customer's need. Begin by listening, then offer relevant options." },
          { text: "Ask them to wait without acknowledging them.", score: 1, feedback: "A short acknowledgement and a clear wait estimate make the service feel respectful." },
        ],
        principle: "Listen before recommending; acknowledge people even when you are busy.",
      },
      {
        prompt: "They need help with an account and begin saying personal details where others can hear.",
        choices: [
          { text: "Move the conversation to a quieter place and confirm only what is needed.", score: 2, feedback: "A discreet setting and minimum necessary details protect the customer's privacy." },
          { text: "Repeat their details loudly so you can check you heard correctly.", score: 0, feedback: "Repeating private details in public exposes information. Verify discreetly using your workplace process." },
          { text: "Ask the next person in line to confirm the account.", score: 0, feedback: "Other customers should not be asked to verify somebody's private information." },
        ],
        principle: "Keep private information private and follow the organization's identity-check process.",
      },
      {
        prompt: "You have two possible solutions, but one is not available today.",
        choices: [
          { text: "Explain the options honestly and agree on the next step the customer prefers.", score: 2, feedback: "Clear choices and honest timing let the customer make an informed decision." },
          { text: "Promise today's availability and hope the issue resolves itself.", score: 0, feedback: "A promise you cannot keep damages trust. Say what you know and verify uncertain details." },
          { text: "Choose for them without explaining the trade-offs.", score: 1, feedback: "A recommendation can help, but explain the trade-offs and leave the customer in control." },
        ],
        principle: "Be accurate, explain options, and confirm the next step.",
      },
    ],
  },
  {
    id: "workplace-spill",
    category: "business",
    title: "Make a safe work area",
    duration: "3 min",
    setting: "A shared shop or stockroom",
    description: "Recognize a hazard, keep others safe, and report it using the local procedure.",
    object: "safety",
    steps: [
      {
        prompt: "You notice an unknown liquid on a busy walkway. What do you do first?",
        choices: [
          { text: "Warn nearby people and keep them away from the area.", score: 2, feedback: "Preventing exposure is the immediate priority. Make the hazard visible and keep people clear." },
          { text: "Walk through it to find out whether it is slippery.", score: 0, feedback: "Testing an unknown spill can expose you to a hazard. Avoid contact and follow site procedure." },
          { text: "Leave it for the next shift without telling anyone.", score: 0, feedback: "An unreported hazard can injure someone. Notify the responsible person promptly." },
        ],
        principle: "Stop access to the hazard before approaching or cleaning it.",
      },
      {
        prompt: "The substance is unidentified and your team has a spill-response procedure.",
        choices: [
          { text: "Follow the procedure, get the trained responder, and use required protective equipment.", score: 2, feedback: "Use the site's approved response and trained people. Do not handle an unknown substance without authorization." },
          { text: "Mix cleaning products to make the cleanup faster.", score: 0, feedback: "Mixing chemicals can create new hazards. Use only approved products and instructions." },
          { text: "Clean it with bare hands because it looks harmless.", score: 0, feedback: "Appearance does not establish safety. Follow the procedure and required protective equipment." },
        ],
        principle: "Do not improvise around unknown substances; use the trained response.",
      },
      {
        prompt: "The area is safe again. What completes the response?",
        choices: [
          { text: "Tell the supervisor and record the hazard and response as required.", score: 2, feedback: "Reporting helps the workplace address the cause and document the response." },
          { text: "Say nothing because no one was injured.", score: 0, feedback: "Near misses and hazards are useful safety information even when nobody is hurt." },
          { text: "Post a photo of the spill and identify coworkers in a public chat.", score: 0, feedback: "Use the designated reporting channel and avoid unnecessary personal information or public blame." },
        ],
        principle: "Report hazards and near misses through the approved workplace channel.",
      },
    ],
  },
  {
    id: "delivery-check",
    category: "business",
    title: "Check an incoming delivery",
    duration: "3 min",
    setting: "A small business receiving area",
    description: "Match a delivery to its order, flag damage, and keep a clear record.",
    object: "delivery",
    steps: [
      {
        prompt: "A delivery arrives during a busy period. What is the first reliable check?",
        choices: [
          { text: "Match the delivery label and items to the purchase order.", score: 2, feedback: "Checking against the order catches wrong items and quantities before stock is accepted." },
          { text: "Sign that everything is correct before opening the boxes.", score: 0, feedback: "Do not confirm details you have not checked. Follow your receiving process." },
          { text: "Put everything straight onto shelves and sort it later.", score: 1, feedback: "Quick storage may seem efficient, but a check first makes discrepancies easier to trace." },
        ],
        principle: "Verify the shipment against its order before recording it as complete.",
      },
      {
        prompt: "One package is visibly crushed and the count is short by one item.",
        choices: [
          { text: "Separate the damaged item, note the shortage and damage, and notify the responsible person.", score: 2, feedback: "Keeping the issue identifiable and documenting it supports a clear supplier follow-up." },
          { text: "Hide the damaged package so the count appears correct.", score: 0, feedback: "Concealing a discrepancy creates inaccurate stock records and delays resolution." },
          { text: "Discard the damaged item before recording what arrived.", score: 0, feedback: "Record the condition and quantity before disposal, following the site's process." },
        ],
        principle: "Preserve evidence of discrepancies and record facts, not guesses.",
      },
      {
        prompt: "The supplier asks for proof of the issue. What record is most useful?",
        choices: [
          { text: "A dated note with the order reference, expected and actual count, and condition.", score: 2, feedback: "Specific, timely facts help resolve the discrepancy and correct inventory." },
          { text: "A memory of what looked wrong, sent several days later.", score: 0, feedback: "Delayed, unrecorded details are harder to verify. Use the receiving log promptly." },
          { text: "A message blaming the driver without describing the package.", score: 0, feedback: "Keep the record factual and describe what you observed rather than assigning blame." },
        ],
        principle: "Keep a timely, factual receiving record and follow the escalation path.",
      },
    ],
  },
  {
    id: "informed-consent",
    category: "research",
    title: "Invite a participant",
    duration: "3 min",
    setting: "A community research project",
    description: "Explain a study clearly, support voluntary choice, and respect a participant's decision.",
    object: "consent",
    steps: [
      {
        prompt: "You invite a community member to a short interview. What should you explain?",
        choices: [
          { text: "The purpose, activities, time, foreseeable risks, and how information will be used.", score: 2, feedback: "A plain-language overview gives people a useful basis for deciding whether to take part." },
          { text: "Only the project title so they are not overwhelmed.", score: 0, feedback: "People need enough relevant information to make a voluntary, informed choice." },
          { text: "That the project guarantees a personal benefit even though you cannot promise one.", score: 0, feedback: "Do not promise benefits that are unknown or outside the study team's control." },
        ],
        principle: "Use the approved study information and explain it in understandable language.",
      },
      {
        prompt: "They ask whether saying no could affect services they receive.",
        choices: [
          { text: "Explain that participation is voluntary and their services do not depend on joining.", score: 2, feedback: "Clear reassurance helps reduce pressure and supports a genuinely voluntary decision." },
          { text: "Suggest they should join to keep the project team happy.", score: 0, feedback: "Pressure undermines voluntary consent. Never imply that services depend on participation." },
          { text: "Skip the question and continue with the interview.", score: 0, feedback: "Pause and answer questions before any study activity begins." },
        ],
        principle: "A person may decline without penalty; do not use authority or services to pressure them.",
      },
      {
        prompt: "They agree to hear more but have not completed the approved consent process.",
        choices: [
          { text: "Answer questions and complete the approved consent steps before collecting study data.", score: 2, feedback: "Interest is not a substitute for consent. Follow the approved process before research activities." },
          { text: "Start recording now and ask for consent at the end.", score: 0, feedback: "Do not collect research data before the required consent process is complete." },
          { text: "Fill in the form for them to save time.", score: 0, feedback: "Consent documentation must reflect the participant's own voluntary decision and the approved process." },
        ],
        principle: "Follow the approved protocol and ethics review; pause if anything is unclear.",
      },
    ],
  },
  {
    id: "sample-custody",
    category: "research",
    title: "Track a research sample",
    duration: "3 min",
    setting: "A supervised field research station",
    description: "Use approved labels, document custody, and pause when sample identity is uncertain.",
    object: "sample",
    steps: [
      {
        prompt: "Two samples are ready for labeling. What is the safest routine?",
        choices: [
          { text: "Follow the study protocol, label one sample at a time, and verify its assigned code.", score: 2, feedback: "A one-at-a-time check reduces mix-ups and protects the link between sample and record." },
          { text: "Label both containers from memory after collection.", score: 0, feedback: "Memory-based labeling increases misidentification risk. Follow the protocol as each sample is handled." },
          { text: "Write a participant's full name on the outside for convenience.", score: 0, feedback: "Use the approved coded identifier. Do not expose direct identifiers unless the protocol specifically requires it." },
        ],
        principle: "Use the approved identifier and protect participant information.",
      },
      {
        prompt: "A label does not match the collection log.",
        choices: [
          { text: "Pause, keep the item secure, document the mismatch, and ask the supervisor.", score: 2, feedback: "Do not guess which identity is correct. A controlled pause protects data integrity." },
          { text: "Change the label to whichever code seems most likely.", score: 0, feedback: "An unsupported correction can permanently misattribute a sample." },
          { text: "Discard the log entry and carry on.", score: 0, feedback: "Do not erase a discrepancy. Preserve the record and use the approved correction process." },
        ],
        principle: "Never resolve identity uncertainty by guessing or silently changing records.",
      },
      {
        prompt: "You hand the sealed sample to the next authorized team member.",
        choices: [
          { text: "Record the transfer, time, condition, and recipient using the study's chain-of-custody log.", score: 2, feedback: "A contemporaneous transfer record makes the sample's handling traceable." },
          { text: "Leave it on a shared table with no handoff note.", score: 0, feedback: "An undocumented handoff creates a gap in custody and handling history." },
          { text: "Send the sample using an unapproved storage method to save time.", score: 0, feedback: "Use only storage and transfer methods specified by the approved protocol." },
        ],
        principle: "Follow the approved handling, storage, and handoff procedure.",
      },
    ],
  },
  {
    id: "field-observation",
    category: "research",
    title: "Record a field observation",
    duration: "3 min",
    setting: "A local environmental observation route",
    description: "Collect consistent measurements and preserve unusual results without changing the data.",
    object: "field",
    steps: [
      {
        prompt: "Your team begins a repeated measurement route. What supports comparable data?",
        choices: [
          { text: "Check the instrument and follow the same approved measurement steps at each site.", score: 2, feedback: "Consistent methods and a basic instrument check help make repeated observations comparable." },
          { text: "Change the measurement method at each site to save time.", score: 0, feedback: "Changing methods mid-study can make comparisons invalid. Follow the protocol." },
          { text: "Skip the instrument check because it worked last week.", score: 1, feedback: "Previous performance is useful context, but follow the protocol's checks for today's collection." },
        ],
        principle: "Follow the protocol consistently and record instrument or method issues.",
      },
      {
        prompt: "One reading is much higher than the others. What do you record?",
        choices: [
          { text: "Keep the reading, record units and context, and flag it for review.", score: 2, feedback: "An unusual observation is data. Record it honestly and use the analysis plan to assess it." },
          { text: "Delete it because it makes the results look inconsistent.", score: 0, feedback: "Removing data without an approved reason introduces bias and weakens trust." },
          { text: "Change the value to fit the expected pattern.", score: 0, feedback: "Never alter an observation to match expectations. Keep the original and report concerns." },
        ],
        principle: "Record what happened; do not edit results to fit a hypothesis.",
      },
      {
        prompt: "A visitor asks to be included in your field notes with their name and photo.",
        choices: [
          { text: "Follow the approved protocol and privacy plan; do not collect extra identifying details.", score: 2, feedback: "Collect only approved information and follow the consent and privacy plan for any identifiable data." },
          { text: "Add the details because the visitor offered them casually.", score: 0, feedback: "A casual offer does not override the approved research and privacy procedures." },
          { text: "Post the photo publicly to show the project is active.", score: 0, feedback: "Do not publish identifiable research information unless it is expressly approved and consented to." },
        ],
        principle: "Minimize personal data and follow the approved consent, privacy, and publication plan.",
      },
    ],
  },
];
