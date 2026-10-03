"""Unvalidated demo questions derived from docs05 and the supplied scenario fixtures."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
# Every question and option has an explicit stable semantic ID, independent of order.
BANKS = {
 'fire': [
  ('recognition', 'In this demo, what should you do when the marked fire hazard appears?', 'recognize', [('recognize', 'Identify the marked hazard'), ('ignore', 'Ignore it'), ('continue_work', 'Continue the task without responding')], 'The fixture starts with hazard recognition.', 'fire.hazard_recognition'),
  ('alarm', 'Which action satisfies the alarm step in this demo?', 'raise', [('walk_away', 'Move away without notification'), ('raise', 'Raise the alarm'), ('skip', 'Skip directly to the result')], 'The configured alarm action must be completed.', 'fire.alarm'),
  ('equipment_permission', 'When does this demo permit the training equipment interaction?', 'configured', [('always', 'In every fire regardless of conditions'), ('configured', 'When this scenario explicitly permits it'), ('unreviewed', 'Whenever a trainee chooses it')], 'Real equipment decisions require approved site content; the demo only permits its configured interaction.', 'fire.equipment'),
  ('route', 'Which route should be selected in the evacuation step?', 'safe', [('blocked', 'The marked blocked route'), ('hazard', 'A route back into the hazard'), ('safe', 'The marked safe exit and muster route')], 'The supplied fixture identifies a safe exit and treats blocked-route selection as a critical error.', 'fire.evacuation'),
  ('reentry', 'How is re-entering the hazard treated by this scenario?', 'critical', [('bonus', 'A bonus action'), ('critical', 'A configured critical error'), ('required', 'A required completion step')], 'The re-entry action is marked criticalFail in the supplied fixture.', 'fire.evacuation'),
  ('sequence', 'Which comes after recognizing the hazard in the configured sequence?', 'alarm', [('certificate', 'Certificate issuance'), ('muster_first', 'Skip the alarm and go straight to the result'), ('alarm', 'The alarm step')], 'The shared scenario definition transitions from recognition to alarm.', 'fire.sequence'),
  ('muster', 'Which action completes the configured Fire practice?', 'muster', [('muster', 'Reach the safe exit and muster area'), ('stop_midway', 'Stop before completing the route'), ('blocked', 'Select the blocked route')], 'The terminal action combines safe exit and muster in this demo fixture.', 'fire.evacuation'),
  ('approved_content', 'What governs real equipment and response decisions at a worksite?', 'site_sop', [('demo_alone', 'This unvalidated app demo alone'), ('site_sop', 'Approved site procedures and authorized training'), ('score_only', 'The numeric demo score alone')], 'This prototype reinforces approved procedures; it does not authorize real-world operations.', 'fire.equipment'),
 ],
 'gas': [
  ('detector', 'What is the first configured response to the demo detector alarm?', 'inspect', [('ignore', 'Ignore the alarm'), ('inspect', 'Inspect and recognize the alarm'), ('enter', 'Enter the restricted area')], 'The supplied fixture begins with recognizing the detector alarm.', 'gas.alarm'),
  ('threshold', 'Does this demo alarm provide a real safe-entry gas threshold?', 'no', [('yes', 'Yes, it supplies a universal threshold'), ('guess', 'A threshold can be guessed from its colour'), ('no', 'No; real thresholds must come from approved site content')], 'The detector state is DEMO_ALARM_NO_THRESHOLD.', 'gas.alarm'),
  ('visible_gas', 'Can a training boundary prove that gas outside it is safe or visibly detectable?', 'no', [('no', 'No; it is a training overlay, not a measurement'), ('yes', 'Yes, the overlay is a gas sensor'), ('colour', 'Yes, gas safety can be decided by colour alone')], 'The boundary is a teaching aid; the app does not measure real gas.', 'gas.hazard_zone'),
  ('entry', 'How is unauthorized restricted-area entry treated in this demo?', 'critical', [('optional', 'An optional shortcut'), ('critical', 'A configured critical error'), ('bonus', 'A bonus for speed')], 'The enter_restricted_zone action is marked criticalFail.', 'gas.confined_space'),
  ('ppe', 'Which PPE choice is correct in this software fixture?', 'configured', [('any', 'Any available option'), ('unapproved', 'The unapproved demo option'), ('configured', 'The explicitly configured demo PPE option')], 'Actual PPE selection must use approved site procedures and equipment requirements.', 'gas.ppe'),
  ('buddy', 'Which choice completes the buddy / attendant step?', 'confirm', [('alone', 'Proceed alone'), ('confirm', 'Confirm the configured buddy / attendant sequence'), ('skip', 'Skip the step')], 'The fixture requires the configured buddy sequence and marks proceeding alone as critical.', 'gas.buddy_system'),
  ('evacuate', 'Which action completes this Gas scenario?', 'report_safe', [('report_safe', 'Report and use the configured safe route'), ('restricted', 'Use the restricted route'), ('ignore', 'Ignore reporting and the route')], 'The terminal action combines reporting and the marked safe route.', 'gas.evacuation'),
  ('authority', 'What must define real confined-space entry rules?', 'approved', [('demo_score', 'The demo score alone'), ('approved', 'Approved site procedures and authorized personnel'), ('guess', 'An unreviewed guess based on this app')], 'This unvalidated prototype does not grant entry authorization.', 'gas.confined_space'),
 ]
}


def main():
    strings = {}
    destination = ROOT / 'mobile-unity/Assets/StreamingAssets/Content/questions'
    destination.mkdir(parents=True, exist_ok=True)
    for module, items in BANKS.items():
        questions = []
        for key, prompt, correct, options, explanation, tag in items:
            stable = module + '.question.' + key
            strings[stable + '.prompt'] = prompt
            strings[stable + '.explanation'] = explanation
            encoded = []
            for option_id, text in options:
                strings[stable + '.option.' + option_id] = text
                encoded.append({'id': stable + '.' + option_id, 'textKey': stable + '.option.' + option_id})
            questions.append({'id': stable, 'promptKey': stable + '.prompt', 'options': encoded, 'correctOptionIds': [stable + '.' + correct], 'explanationKey': stable + '.explanation', 'topicTags': [tag], 'difficulty': 'application'})
        bank = {'questionBankId': module + '-questions-v1', 'moduleId': 'fire-response' if module == 'fire' else 'gas-confined-space', 'version': '1.0.0', 'questions': questions}
        (destination / (module + '_v1.json')).write_text(json.dumps(bank, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (ROOT / 'demo/localization/question-copy-en.json').write_text(json.dumps(strings, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Wrote two 8-question demo banks and 80 localized reference keys; human review required.')


if __name__ == '__main__':
    main()
