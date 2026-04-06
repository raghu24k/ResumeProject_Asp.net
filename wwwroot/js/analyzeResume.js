/**
 * ResumeAI – Analyze AI (in-app resume analysis)
 * ──────────────────────────────────────────────────
 * Provides the analyzeAI(resumeId) function that:
 *   1. Sends the resume ID to /Analyze/AnalyzeResume
 *   2. Displays a full-screen modal with AI analysis results
 */

function analyzeAI(resumeId) {
    if (!resumeId) {
        alert('No resume ID found. Please save your resume first.');
        return;
    }

    // Create and show the modal
    showAnalyzeModal();
    setAnalyzeLoading(true);

    fetch('/Analyze/AnalyzeResume', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ resumeId: resumeId })
    })
        .then(function (response) {
            if (!response.ok) {
                return response.json().then(function (err) { throw new Error(err.error || 'Server error'); });
            }
            return response.json();
        })
        .then(function (data) {
            setAnalyzeLoading(false);
            renderAnalyzeResults(data);
        })
        .catch(function (err) {
            setAnalyzeLoading(false);
            renderAnalyzeError(err.message || 'Something went wrong. Please try again.');
        });
}

function showAnalyzeModal() {
    // Remove existing modal if any
    var existing = document.getElementById('aiAnalyzeOverlay');
    if (existing) existing.remove();

    var overlay = document.createElement('div');
    overlay.id = 'aiAnalyzeOverlay';
    overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.85);backdrop-filter:blur(8px);z-index:9999;display:flex;align-items:center;justify-content:center;overflow-y:auto;padding:20px 0;';

    overlay.innerHTML =
        '<div id="aiAnalyzeModal" style="background:#13131a;border:1px solid rgba(255,255,255,0.08);width:90%;max-width:860px;border-radius:24px;overflow:hidden;position:relative;margin:auto;animation:aiModalSlideUp 0.4s ease-out;">' +
        '<button onclick="closeAnalyzeModal()" style="position:absolute;top:20px;right:24px;background:none;border:none;color:#6b6588;font-size:24px;cursor:pointer;z-index:10;">✕</button>' +
        '<div id="aiAnalyzeBody" style="padding:32px 40px;"></div>' +
        '</div>';

    // Add animation keyframes
    var style = document.createElement('style');
    style.textContent = '@keyframes aiModalSlideUp { from { transform: translateY(40px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }';
    overlay.appendChild(style);

    document.body.appendChild(overlay);
    document.body.style.overflow = 'hidden';
}

function closeAnalyzeModal() {
    var overlay = document.getElementById('aiAnalyzeOverlay');
    if (overlay) overlay.remove();
    document.body.style.overflow = 'auto';
}

function setAnalyzeLoading(isLoading) {
    var body = document.getElementById('aiAnalyzeBody');
    if (!body) return;

    if (isLoading) {
        body.innerHTML =
            '<div style="text-align:center;padding:60px 20px;">' +
            '<div style="width:48px;height:48px;border:4px solid rgba(139,92,246,0.2);border-top-color:#8b5cf6;border-radius:50%;animation:spin 0.8s linear infinite;margin:0 auto 20px;"></div>' +
            '<h3 style="font-size:20px;font-weight:700;color:#f1f0f7;margin-bottom:8px;">Analyzing your resume...</h3>' +
            '<p style="color:#6b6588;font-size:14px;">Our AI is reviewing your resume against ATS standards</p>' +
            '</div>' +
            '<style>@keyframes spin { to { transform: rotate(360deg); } }</style>';
    }
}

function renderAnalyzeError(message) {
    var body = document.getElementById('aiAnalyzeBody');
    if (!body) return;

    body.innerHTML =
        '<div style="text-align:center;padding:60px 20px;">' +
        '<div style="width:64px;height:64px;background:rgba(239,68,68,0.12);border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 20px;font-size:28px;">❌</div>' +
        '<h3 style="font-size:20px;font-weight:700;color:#f87171;margin-bottom:8px;">Analysis Failed</h3>' +
        '<p style="color:#6b6588;font-size:14px;max-width:400px;margin:0 auto;">' + _esc(message) + '</p>' +
        '<button onclick="closeAnalyzeModal()" style="margin-top:20px;background:linear-gradient(135deg,#8b5cf6,#ec4899);color:#fff;border:none;padding:10px 28px;border-radius:50px;cursor:pointer;font-weight:600;">Close</button>' +
        '</div>';
}

function renderAnalyzeResults(data) {
    var body = document.getElementById('aiAnalyzeBody');
    if (!body) return;

    var score = data.atsScore || 0;
    var scoreColor = score >= 80 ? '#10b981' : score >= 60 ? '#f59e0b' : '#ef4444';
    var scoreBg = score >= 80 ? 'rgba(16,185,129,0.2)' : score >= 60 ? 'rgba(245,158,11,0.2)' : 'rgba(239,68,68,0.2)';
    var scoreBorder = score >= 80 ? 'rgba(16,185,129,0.3)' : score >= 60 ? 'rgba(245,158,11,0.3)' : 'rgba(239,68,68,0.3)';
    var scoreLabel = score >= 80 ? 'Excellent' : score >= 60 ? 'Good' : 'Needs Work';

    var offset = 314 - (314 * score / 100);

    var html = '';

    // Header
    html += '<div style="display:flex;align-items:center;gap:12px;margin-bottom:24px;">';
    html += '<h2 style="font-size:24px;font-weight:700;margin:0;color:#f1f0f7;">Resume Analysis</h2>';
    html += '<span style="background:' + scoreBg + ';color:' + scoreColor + ';font-size:12px;font-weight:700;padding:4px 12px;border-radius:50px;border:1px solid ' + scoreBorder + ';">ATS Score: ' + score + '/100 — ' + scoreLabel + '</span>';
    html += '</div>';

    // Score circle
    html += '<div style="text-align:center;margin-bottom:28px;">';
    html += '<div style="position:relative;width:120px;height:120px;margin:0 auto;">';
    html += '<svg width="120" height="120" viewBox="0 0 120 120">';
    html += '<circle cx="60" cy="60" r="50" stroke="rgba(255,255,255,0.06)" stroke-width="10" fill="none"/>';
    html += '<circle id="aiScoreCircle" cx="60" cy="60" r="50" stroke="' + scoreColor + '" stroke-width="10" fill="none" stroke-linecap="round" stroke-dasharray="314" stroke-dashoffset="314" transform="rotate(-90 60 60)" style="transition:stroke-dashoffset 1.5s ease;"/>';
    html += '</svg>';
    html += '<div style="position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);text-align:center;">';
    html += '<div style="font-size:28px;font-weight:800;color:#fff;">' + score + '</div>';
    html += '<div style="font-size:10px;color:#6b6588;">ATS Score</div>';
    html += '</div></div></div>';

    // Summary
    html += '<div style="background:rgba(139,92,246,0.06);border:1px solid rgba(139,92,246,0.15);border-radius:16px;padding:24px;margin-bottom:28px;">';
    html += '<div style="display:flex;align-items:center;gap:8px;font-size:14px;font-weight:700;color:#a78bfa;margin-bottom:12px;">💡 Overall Summary</div>';
    html += '<p style="color:#a09bb8;font-size:14px;line-height:1.7;margin:0;">' + _esc(data.summary || '') + '</p>';
    html += '</div>';

    // Strengths & Improvements grid
    html += '<div style="display:grid;grid-template-columns:1fr 1fr;gap:28px;margin-bottom:28px;">';

    // Strengths
    html += '<div><h3 style="display:flex;align-items:center;gap:8px;font-size:16px;font-weight:700;color:#10b981;margin-bottom:16px;">✔ Strengths</h3>';
    html += '<div style="display:flex;flex-direction:column;gap:8px;">';
    (data.strengths || []).forEach(function (s) {
        html += '<div style="background:rgba(16,185,129,0.06);border:1px solid rgba(16,185,129,0.15);border-radius:12px;padding:12px 16px;font-size:13px;color:#a09bb8;display:flex;gap:12px;line-height:1.5;"><span style="color:#10b981;font-weight:bold;flex-shrink:0;">✔</span>' + _esc(s) + '</div>';
    });
    html += '</div></div>';

    // Improvements
    html += '<div><h3 style="display:flex;align-items:center;gap:8px;font-size:16px;font-weight:700;color:#ef4444;margin-bottom:16px;">✕ Areas for Improvement</h3>';
    html += '<div style="display:flex;flex-direction:column;gap:8px;">';
    (data.improvements || []).forEach(function (s) {
        html += '<div style="background:rgba(239,68,68,0.04);border:1px solid rgba(239,68,68,0.15);border-radius:12px;padding:12px 16px;font-size:13px;color:#a09bb8;display:flex;gap:12px;line-height:1.5;"><span style="color:#ef4444;font-weight:bold;flex-shrink:0;">✕</span>' + _esc(s) + '</div>';
    });
    html += '</div></div>';
    html += '</div>';

    // Keywords
    html += '<div style="margin-bottom:24px;"><h3 style="display:flex;align-items:center;gap:8px;font-size:16px;font-weight:700;color:#f59e0b;margin-bottom:12px;">🔑 Suggested Keywords</h3>';
    html += '<div style="display:flex;flex-wrap:wrap;gap:8px;">';
    (data.keywordSuggestions || []).forEach(function (k) {
        html += '<span style="background:rgba(245,158,11,0.1);border:1px solid rgba(245,158,11,0.25);color:#f59e0b;padding:5px 14px;border-radius:50px;font-size:12px;font-weight:600;">' + _esc(k) + '</span>';
    });
    html += '</div></div>';

    // Formatting tips
    html += '<div><h3 style="display:flex;align-items:center;gap:8px;font-size:16px;font-weight:700;color:#3b82f6;margin-bottom:12px;">📐 Formatting Tips</h3>';
    html += '<div style="display:flex;flex-direction:column;gap:8px;">';
    (data.formattingTips || []).forEach(function (t) {
        html += '<div style="background:rgba(59,130,246,0.06);border:1px solid rgba(59,130,246,0.15);border-radius:12px;padding:12px 16px;font-size:13px;color:#a09bb8;display:flex;gap:12px;line-height:1.5;">📐 ' + _esc(t) + '</div>';
    });
    html += '</div></div>';

    body.innerHTML = html;

    // Animate the score circle after render
    setTimeout(function () {
        var circle = document.getElementById('aiScoreCircle');
        if (circle) circle.style.strokeDashoffset = offset;
    }, 100);
}