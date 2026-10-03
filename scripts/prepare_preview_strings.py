"""Author reference copy and explicitly marked translation drafts for the preview."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EN = {
    'ui.brand': 'SURAKSHA XR', 'ui.tagline': 'Practice today. Return home safely.',
    'ui.preview': 'DEVELOPMENT PREVIEW', 'ui.offline': 'Training available offline',
    'ui.language': 'Choose your language', 'ui.worker': 'Choose a worker', 'ui.add_worker': 'Add worker',
    'ui.worker_code': 'Worker code', 'ui.display_name': 'Display name', 'ui.save': 'Save profile',
    'ui.home': 'Training home', 'ui.back': 'Back', 'ui.settings': 'Settings & safety notice',
    'ui.history': 'Training history', 'ui.certificates': 'Certificates', 'ui.empty_history': 'Your completed practice sessions will appear here.',
    'ui.empty_certificates': 'No certificates yet. Complete a passing assessment to receive a signed demo certificate.',
    'ui.safety': 'Unvalidated demo content. This preview is not an approved site procedure or a statutory certificate. Follow your site SOP and authorized trainer.',
    'ui.translation_notice': 'Translations are draft content requiring human and safety review. Santali entries are clearly marked English review placeholders.',
    'ui.practice': 'Start practice', 'ui.practice_done': 'Practice completed',
    'ui.assessment_pending': 'Knowledge assessment and certification are under development.',
    'ui.fallback': 'Use the offline 3D simulator, or choose AR on a supported device with its AR service already installed.',
    'ui.ar_mode': 'AR tabletop training', 'ui.simulator': 'Use 3D simulator',
    'ui.ar_checking': 'Checking AR on this device…',
    'ui.ar_unavailable': 'AR is unavailable or its service is not installed. Continue offline in 3D.',
    'ui.ar_scan': 'Point at a clear, well-lit floor or table. Move the phone slowly, then tap the surface.',
    'ui.ar_surface': 'Surface found. Confirm placement to begin.',
    'ui.ar_tracking': 'Tracking paused. Point the camera back at the training surface.',
    'ui.ar_timeout': 'No stable surface yet. Try better lighting or continue in 3D.',
    'ui.ar_place': 'Confirm & start AR', 'ui.ar_reset': 'Reset placement',
    'ui.ar_denied': 'Camera permission denied. Training is available offline in 3D.',
    'ui.ar_help': 'Keep your surroundings clear. This miniature scene uses the same actions and scoring as 3D. Tap a surface on the left to select the origin, then confirm.',
    'ui.controls': 'Drag the joystick to walk. Drag the scene above it to look. Approach the object, then choose an action.',
    'ui.pause': 'Pause', 'ui.resume': 'Resume', 'ui.exit': 'Exit practice', 'ui.paused': 'Practice paused',
    'ui.continue': 'Continue', 'ui.retry': 'Try again', 'ui.result': 'Practice summary',
    'ui.saved': 'Saved on this device', 'ui.score': 'Scenario score', 'ui.weak_topics': 'Topics to revisit',
    'ui.no_weak_topics': 'No weak topics found in this session.', 'ui.again': 'Practice again',
    'ui.distance': 'Approach the highlighted object to interact.', 'ui.interact': 'Choose an action',
    'ui.error': 'Could not complete this operation. Your existing records are preserved. Restart and try again.',
    'ui.invalid_profile': 'Enter a unique worker code and a display name.', 'ui.pending': 'Records waiting to sync',
    'ui.local_data': 'Profiles and practice history are saved locally. Sync is not enabled in this preview.',
    'ui.none': 'None', 'ui.demo': 'DEMO • NOT AN APPROVED SOP',
    'fire.module.title': 'Fire & Explosion Response', 'fire.module.description': 'Recognize the hazard, raise the alarm, make the configured demo decision and reach the muster area.',
    'gas.module.title': 'Gas Leak & Confined Space', 'gas.module.description': 'Recognize the demo detector alarm, restricted area, equipment and buddy sequence, then take the safe route.',
    'common.safe_exit': 'Safe exit', 'common.blocked_exit': 'Blocked exit', 'common.safe_route': 'Safe route',
    'fire.entity.fire': 'Demo fire hazard', 'fire.entity.alarm': 'Alarm panel', 'fire.entity.extinguisher': 'Demo equipment',
    'gas.entity.detector': 'Demo detector', 'gas.entity.restricted_zone': 'Restricted area', 'gas.entity.ppe': 'Configured demo PPE', 'gas.entity.buddy': 'Buddy / attendant station',
}
STEPS = {
    'fire': {
        'recognize': ('Recognize the fire hazard', 'Observe the marked demo hazard. Choose how to respond.'),
        'alarm': ('Raise the alarm', 'Find the alarm panel and choose the notification action.'),
        'decision': ('Choose the configured response', 'This fixture permits demo equipment use. It does not authorize real firefighting.'),
        'evacuate': ('Use the safe exit and muster area', 'Follow the marked safe route. Do not choose the blocked route or re-enter the hazard.'),
    },
    'gas': {
        'alarm': ('Recognize the detector alarm', 'The detector shows a demo alarm. No gas concentration or real safety threshold is simulated.'),
        'zone': ('Recognize the restricted area', 'Observe the training boundary and choose the restricted-area response.'),
        'ppe': ('Select configured demo PPE', 'Use the option specified by this demo. Real equipment selection requires the site SOP.'),
        'buddy': ('Confirm the buddy / attendant sequence', 'Find the buddy station and choose the configured sequence.'),
        'evacuate': ('Report and use the safe route', 'Use the marked safe route and report the alarm as configured in this demo.'),
    },
}
ACTIONS = {
    'identify_hazard': 'Identify the hazard', 'ignore_hazard': 'Ignore the hazard',
    'raise_alarm': 'Raise the alarm', 'move_without_alarm': 'Move on without raising the alarm',
    'use_demo_extinguisher_when_permitted': 'Use the equipment permitted by this demo', 'take_blocked_route': 'Take the blocked route',
    'reach_safe_exit_and_muster': 'Reach the safe exit and muster area', 'reenter_hazard': 'Re-enter the hazard area',
    'inspect_detector_alarm': 'Inspect the detector alarm', 'ignore_detector_alarm': 'Ignore the detector alarm',
    'recognize_restricted_zone': 'Recognize and remain outside the restricted area', 'enter_restricted_zone': 'Enter the restricted area',
    'select_configured_ppe_option': 'Select the configured demo PPE option', 'select_unapproved_demo_option': 'Select the unapproved demo option',
    'confirm_buddy_attendant_sequence': 'Confirm the buddy / attendant sequence', 'proceed_alone': 'Proceed alone',
    'report_and_use_safe_route': 'Report and use the safe route', 'use_restricted_route': 'Use the restricted route',
}
FEEDBACK = {
    'fire.feedback.hazard_correct': 'Hazard recognized. Continue to the alarm.', 'fire.feedback.hazard_ignored': 'The hazard was missed. Recognize it before continuing.',
    'fire.feedback.alarm_correct': 'Alarm raised in the demo.', 'fire.feedback.alarm_missed': 'Notification was missed. Return to the alarm action.',
    'fire.feedback.equipment_correct': 'The configured demo response was selected.', 'fire.feedback.blocked_route': 'The blocked route is a critical error in this scenario. Retry the configured response.',
    'fire.feedback.evacuation_correct': 'Safe exit and muster sequence completed.', 'fire.feedback.reentry': 'Re-entry is a critical error in this scenario. Use the safe exit.',
    'gas.feedback.alarm_correct': 'Demo detector alarm recognized.', 'gas.feedback.alarm_ignored': 'The alarm was missed. Inspect the detector.',
    'gas.feedback.zone_correct': 'Restricted area recognized.', 'gas.feedback.unauthorized_entry': 'Unauthorized entry is a critical error in this scenario. Stay outside the boundary.',
    'gas.feedback.ppe_correct': 'Configured demo PPE selected.', 'gas.feedback.ppe_incorrect': 'This option is not approved by the demo configuration. Try again.',
    'gas.feedback.buddy_correct': 'Buddy / attendant sequence confirmed.', 'gas.feedback.buddy_missed': 'Proceeding alone is a critical error in this scenario. Confirm the configured sequence.',
    'gas.feedback.evacuate_correct': 'Report and safe-route sequence completed.', 'gas.feedback.evacuate_incorrect': 'The restricted route is a critical error in this scenario. Use the safe route.',
}
HI = {
    'ui.language': 'अपनी भाषा चुनें', 'ui.worker': 'कर्मचारी चुनें', 'ui.add_worker': 'कर्मचारी जोड़ें',
    'ui.worker_code': 'कर्मचारी कोड', 'ui.display_name': 'नाम', 'ui.save': 'प्रोफ़ाइल सहेजें',
    'ui.home': 'प्रशिक्षण', 'ui.back': 'वापस', 'ui.settings': 'सेटिंग और सुरक्षा सूचना',
    'ui.history': 'प्रशिक्षण इतिहास', 'ui.certificates': 'प्रमाणपत्र', 'ui.practice': 'अभ्यास शुरू करें',
    'ui.pause': 'रोकें', 'ui.resume': 'जारी रखें', 'ui.exit': 'अभ्यास छोड़ें', 'ui.paused': 'अभ्यास रुका है',
    'ui.continue': 'आगे बढ़ें', 'ui.retry': 'फिर कोशिश करें', 'ui.result': 'अभ्यास सारांश',
    'ui.saved': 'इस डिवाइस पर सहेजा गया', 'ui.score': 'परिदृश्य अंक', 'ui.again': 'फिर अभ्यास करें',
    'ui.offline': 'प्रशिक्षण ऑफलाइन उपलब्ध है', 'ui.demo': 'डेमो • स्वीकृत कार्यविधि नहीं',
    'fire.module.title': 'आग और विस्फोट प्रतिक्रिया', 'gas.module.title': 'गैस रिसाव और सीमित स्थान',
    'common.safe_exit': 'सुरक्षित निकास', 'common.blocked_exit': 'अवरुद्ध निकास', 'common.safe_route': 'सुरक्षित रास्ता',
}

EN.update({'ui.ar_mode': 'AR ground training', 'ui.ar_scan': 'Scan the training area', 'ui.ar_surface': 'Instructor surface selected. Confirm to anchor the tabletop scene.', 'ui.ar_tracking': 'Move your phone slowly to restore tracking.', 'ui.ar_help': 'Move your phone slowly toward the ground and surroundings. Scan left and right from one spot. The scene starts automatically when enough ground is found. Use a clear area with a trainer.', 'ui.ar_ready': 'Environment Ready', 'ui.ar_running': 'Training area anchored', 'ui.ar_anchoring': 'Securing the training area…', 'ui.ar_anchor_failed': 'Could not anchor this area. Rescan or use 3D simulation.', 'ui.ar_manual_help': 'Instructor fallback: tap a detected surface, then confirm.', 'ui.ar_continue_3d': 'Continue this attempt in 3D', 'ui.ar_timeout': 'Not enough stable ground yet. Scan a wider area, rescan, or use 3D.', 'ui.ar_reset': 'Rescan area', 'ui.open_actions': 'Show actions', 'ui.close_actions': 'Hide actions'})

EN.update({'ui.ar_mode': 'AR training', 'ui.ar_help': 'Move the phone gently across the floor or a clear surface. You do not need to hold still. Smaller spaces automatically use a compact model. Keep your surroundings clear.', 'ui.ar_scan': 'Finding a surface… keep moving gently', 'ui.ar_tracking': 'Finding camera tracking… move gently toward a textured, well-lit surface.', 'ui.ar_timeout': 'No usable surface yet. Try a textured floor or table with better lighting, or start 3D training.', 'ui.ar_ready': 'Training area found', 'ui.ar_compact_ready': 'Compact AR ready • scaled training model', 'ui.offline_badge': 'OFFLINE READY', 'ui.welcome': 'Your training workspace', 'ui.home_guidance': 'Choose a module. Practise each decision, then check your knowledge.', 'ui.safety_module': 'SAFETY MODULE', 'ui.open_module': 'Open training', 'ui.choose_experience': 'Choose your experience', 'ui.practice_label': 'PRACTICE', 'ui.assessment_label': 'ASSESSMENT', 'ui.action_instruction': 'Read the instruction above, then choose an action below.', 'ui.action_recorded': 'Action recorded. Observe the scene response, then continue. Assessment results appear at the end.', 'ui.surfaces': 'Detected surfaces', 'ui.training_sounds': 'Training sounds'})

EN.update({'ui.ar_more_light': 'Move to brighter, even lighting. Keep scanning gently.', 'ui.ar_more_texture': 'Show textured ground or a table. Plain walls, glare and darkness are difficult to track.', 'ui.ar_slow_sweep': 'Slow the sweep slightly. You do not need to hold the phone still.'})

EN.update({
    'ui.layout_safe_zone': 'SAFE ZONE',
    'ui.layout_gas_visual': 'Gas and wind are simulated training visuals.',
    'ui.selected': 'Selected',
    'ui.instructor_auto': 'Instructor: automatic layout',
    'ui.instructor_manual': 'Instructor: manual tabletop',
    'ui.instructor_life_size': 'Instructor: life-size layout',
    'ui.instructor_compact': 'Instructor: compact model',
    'ui.startup_error': 'SurakshaXR could not start. Your existing data is preserved. Restart the app; contact the trainer if the problem continues.',
    'ui.nav_home': 'Train', 'ui.nav_history': 'History', 'ui.nav_certificates': 'Certificates', 'ui.nav_settings': 'Settings',
    'ui.mine_mode': 'Immersive Mine · phone tracking',
    'ui.ar_mode': 'Ground AR · camera surroundings',
    'ui.ar_help': 'Scan the ground and nearby surroundings with a slow sweep. Normal hand movement is fine. Life-size equipment appears automatically on detected ground. Keep a clear walking area.',
    'ui.ar_scan': 'Scan the training area · slowly look across the ground',
    'ui.ar_ready': 'Environment ready · life-size scene',
    'ui.ar_timeout': 'More visible ground is needed for this life-size scene. Scan a wider clear area, or choose the virtual mine or 3D simulator.',
    'ui.ar_more_texture': 'Look across textured ground. Sky, plain walls, glare and darkness are difficult to track.',
    'ui.mine_help': 'A virtual mine surrounds you. Look and walk in a clear area, or travel with the joystick. Use View surroundings to check real obstacles. Follow the bright gold marker for your current task. The floor is virtual, not a room measurement.',
    'ui.mine_tracking': 'Starting phone tracking… move gently and look around',
    'ui.mine_ready': 'Virtual mine ready',
    'ui.view_surroundings': 'View surroundings',
    'ui.mine_return': 'Return to mine',
    'ui.mine_controls': 'Walk or use joystick · follow the gold task marker',
    'ui.mine_boundary': 'Near the virtual mine edge. Turn back or use the joystick; check real surroundings.',
    'ui.mine_camera': 'Camera view · training interaction paused',
})

def main():
    en = dict(EN)
    en.pop('ui.offline_badge', None)
    en.update({
        'ui.assessment': 'Start assessment', 'ui.practice_required': 'Complete practice once to unlock assessment.',
        'topic.fire.equipment': 'Extinguisher readiness and exit access',
        'ui.micro_step': 'Micro-training step', 'ui.start_refresher': 'Start short refresher', 'ui.refresher_complete': 'Refresher completed',
        'ui.scan_qr': 'Scan QR with camera', 'ui.scan_help': 'Hold the entire QR code inside the camera view.',
        'ui.camera_denied': 'Camera permission denied. You can paste the QR payload below to verify offline.',
        'ui.camera_unavailable': 'Camera unavailable. Paste the QR payload below to verify offline.',
        'ui.reminder_help': 'Optional reminders tell you when short refresher training is due. They work offline. Android battery settings can delay delivery; due dates remain visible in Refreshers.',
        'ui.enable_reminders': 'Enable offline reminders', 'ui.disable_reminders': 'Disable reminders',
        'ui.reminders_enabled': 'Reminders enabled', 'ui.reminders_disabled': 'Reminders disabled', 'ui.reminders_denied': 'Notification permission denied. Refreshers remain available in the app.',
        'ui.reminder_title': 'SurakshaXR refresher', 'ui.reminder_message': 'Short refresher training is due. Open SurakshaXR to train offline.',
        'ui.knowledge': 'Knowledge assessment', 'ui.submit_answer': 'Submit answer', 'ui.choose_answer': 'Choose an answer to continue.',
        'ui.passed': 'Passed', 'ui.failed': 'Needs retraining', 'ui.refresher': 'Refresher',
        'ui.result': 'Training result', 'ui.score': 'Score', 'ui.exit': 'Leave training', 'ui.paused': 'Training paused',
        'ui.certificate': 'Training Competency Certificate',
        'ui.certificate_notice': 'DEMO ONLY. Legal recognition depends on the authorized issuing organization and approved training program. This prototype uses a demo issuer; replace it with provisioned production keys.',
        'ui.verify': 'Verify certificate offline', 'ui.verify_help': 'Paste a certificate QR payload. Verification uses the signed trust bundle stored on this device.',
        'ui.copy_qr': 'Copy QR payload', 'ui.trust_updated': 'Trust data last updated',
        'ui.refreshers': 'Refreshers', 'ui.no_refreshers': 'Complete an assessment to schedule your refresher.', 'ui.due': 'Refresher due',
        'verify.VERIFIED_TRUSTED': 'Signature verified · trusted demo issuer',
        'verify.UNKNOWN_SIGNER_UNVERIFIED': 'Unknown issuer · signature could not be verified',
        'verify.SIGNER_NOT_TRUSTED': 'Signature valid · issuer revoked or outside validity period',
        'verify.INVALID_SIGNATURE': 'Invalid signature · certificate may have been altered',
        'verify.UNSUPPORTED_VERSION': 'Unsupported certificate format', 'verify.PARSE_ERROR': 'Invalid or incomplete QR payload',
    })
    question_copy = ROOT / 'demo/localization/question-copy-en.json'
    if question_copy.exists():
        en.update(json.loads(question_copy.read_text(encoding='utf-8')))
    en.update(FEEDBACK)
    for action, text in ACTIONS.items():
        en['action.' + action] = text
    for module, steps in STEPS.items():
        for step, (objective, narration) in steps.items():
            en[f'{module}.step.{step}.objective'] = objective
            en[f'{module}.step.{step}.narration'] = narration
        path = ROOT / f'examples/demo_content/scenarios/{module}_v1.example.json'
        scenario = json.loads(path.read_text(encoding='utf-8'))
        en[scenario['titleKey']] = en[f'{module}.module.title']
        en[scenario['descriptionKey']] = en[f'{module}.module.description']
        for key in scenario['objectives']:
            en[key] = key.rsplit('.', 1)[-1].replace('_', ' ').capitalize()
        for key in scenario['scoring']['categories']:
            en['category.' + key] = key.replace('_', ' ').capitalize()
        for step in scenario['steps']:
            for action in step['allowedActions']:
                for key in action['weakTopicTags']:
                    en['topic.' + key] = key.rsplit('.', 1)[-1].replace('_', ' ').capitalize()
    hindi = dict(HI)
    hindi.update(json.loads((ROOT / 'demo/localization/hi-draft.json').read_text(encoding='utf-8')))
    for module in ('fire', 'gas'):
        for suffix in ('title', 'description'):
            hindi[f'{module}.scenario.demo01.{suffix}'] = hindi[f'{module}.module.{suffix}']
    missing = set(en) - set(hindi)
    if missing:
        raise ValueError('Hindi draft keys missing: ' + ', '.join(sorted(missing)))
    tables = {'en': en, 'hi': {k: hindi[k] for k in en}, 'sat': {k: '[SAT REVIEW] ' + v for k, v in en.items()}}
    output = ROOT / 'demo/localization'
    output.mkdir(parents=True, exist_ok=True)
    for locale, table in tables.items():
        (output / (locale + '.json')).write_text(json.dumps(table, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Wrote {len(en)} keys in three locales. Translation review is required.')

if __name__ == '__main__':
    main()
