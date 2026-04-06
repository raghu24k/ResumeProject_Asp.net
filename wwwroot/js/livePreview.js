/**
 * ResumeAI – Live Preview Engine
 * ────────────────────────────────
 * Listens to every input/textarea/select inside the "Add …" forms and
 * mirrors the data into the right-hand resume preview panel in real time.
 *
 * Each section page only needs to call the appropriate init function.
 */

// ═══════════════════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════════════════

function _el(id) { return document.getElementById(id); }

function _val(id) {
    var el = _el(id);
    return el ? el.value.trim() : '';
}

function _ensureSection(containerId, sectionTitle, afterSelector) {
    // Returns (or creates) a wrapper div inside the preview for a given section
    var container = _el(containerId);
    if (container) return container;

    var preview = document.querySelector('.resume-paper');
    if (!preview) return null;

    // Build section title + container
    var titleDiv = document.createElement('div');
    titleDiv.className = 'rp-section-title';
    titleDiv.id = containerId + '_title';
    titleDiv.textContent = sectionTitle;
    titleDiv.style.display = 'none';

    container = document.createElement('div');
    container.id = containerId;

    // Try to insert after the afterSelector element, otherwise just append
    if (afterSelector) {
        var ref = preview.querySelector(afterSelector);
        if (ref && ref.nextSibling) {
            preview.insertBefore(titleDiv, ref.nextSibling);
            preview.insertBefore(container, titleDiv.nextSibling);
            return container;
        }
    }
    preview.appendChild(titleDiv);
    preview.appendChild(container);
    return container;
}

function _showHideTitle(containerId) {
    var titleEl = _el(containerId + '_title');
    var containerEl = _el(containerId);
    if (!titleEl || !containerEl) return;
    var hasContent = containerEl.innerHTML.trim().length > 0;
    titleEl.style.display = hasContent ? '' : 'none';
}

// ═══════════════════════════════════════════════════════════════════════
// PERSONAL DETAILS
// ═══════════════════════════════════════════════════════════════════════

function initPersonalDetailsPreview() {
    var fields = ['firstName', 'lastName', 'emailField', 'phoneField',
        'locationField', 'linkedinField', 'portfolioField', 'summaryField'];
    fields.forEach(function (fid) {
        var el = _el(fid);
        if (el) el.addEventListener('input', _updatePersonalPreview);
    });
    // Run once to set initial state
    _updatePersonalPreview();
}

function _updatePersonalPreview() {
    var fn = _val('firstName');
    var ln = _val('lastName');
    var em = _val('emailField');
    var ph = _val('phoneField');
    var lo = _val('locationField');
    var sm = _val('summaryField');

    var nameEl = _el('prevName');
    if (nameEl) nameEl.textContent = (fn + ' ' + ln).trim() || 'Your Name';

    var contactEl = _el('prevContact');
    if (contactEl) {
        var parts = [em, ph, lo].filter(function (x) { return x !== ''; });
        contactEl.textContent = parts.join(' · ');
    }

    var li = _val('linkedinField');
    var po = _val('portfolioField');
    var linksEl = _el('prevLinks');
    if (linksEl) {
        var linkParts = [li, po].filter(function (x) { return x !== ''; });
        linksEl.textContent = linkParts.join(' · ');
        linksEl.style.display = linkParts.length > 0 ? '' : 'none';
    }

    // Handle summary section – create if needed
    var summarySection = _el('prevSummarySection');
    var summaryP = _el('prevSummary');

    if (!summarySection) {
        // Create the section if it doesn't exist
        var preview = document.querySelector('.resume-paper');
        if (preview) {
            var contactDiv = _el('prevContact');
            summarySection = document.createElement('div');
            summarySection.id = 'prevSummarySection';

            var stitle = document.createElement('div');
            stitle.className = 'rp-section-title';
            stitle.textContent = 'Profile';
            summarySection.appendChild(stitle);

            summaryP = document.createElement('p');
            summaryP.id = 'prevSummary';
            summaryP.style.cssText = 'color:#444;font-size:10px;';
            summarySection.appendChild(summaryP);

            if (contactDiv && contactDiv.nextSibling) {
                preview.insertBefore(summarySection, contactDiv.nextSibling);
            } else {
                preview.appendChild(summarySection);
            }
        }
    }

    if (summarySection && summaryP) {
        summaryP.textContent = sm;
        summarySection.style.display = sm ? '' : 'none';
    }
}

// ═══════════════════════════════════════════════════════════════════════
// EXPERIENCE – live preview of the "Add New" form
// ═══════════════════════════════════════════════════════════════════════

function initExperiencePreview() {
    var fields = ['expCompany', 'expJobTitle', 'expStart', 'expEnd', 'expCurrent', 'expDesc'];
    fields.forEach(function (fid) {
        var el = _el(fid);
        if (el) {
            var evt = (el.type === 'checkbox') ? 'change' : 'input';
            el.addEventListener(evt, _updateExpPreview);
        }
    });
    _updateExpPreview();
}

function _updateExpPreview() {
    var company = _val('expCompany');
    var jobTitle = _val('expJobTitle');
    var startDate = _val('expStart');
    var endDate = _val('expEnd');
    var isCurrent = _el('expCurrent') && _el('expCurrent').checked;
    var desc = _val('expDesc');

    var hasData = company || jobTitle || desc;

    // Ensure container exists
    var container = _ensureSection('liveExpContainer', 'Experience (typing...)', '#prevSummarySection');
    if (!container) container = _ensureSection('liveExpContainer', 'Experience (typing...)');
    if (!container) return;

    if (!hasData) {
        container.innerHTML = '';
        _showHideTitle('liveExpContainer');
        return;
    }

    var startStr = startDate ? _formatDate(startDate) : '';
    var endStr = isCurrent ? 'Present' : (endDate ? _formatDate(endDate) : '');
    var dateStr = [startStr, endStr].filter(Boolean).join(' - ');

    container.innerHTML =
        '<div class="rp-item" style="opacity:0.7;border-left:2px solid #7c3aed;padding-left:6px;">' +
        '<span class="rp-item-title">' + _esc(jobTitle) + '</span>' +
        (dateStr ? '<span class="rp-item-date">' + _esc(dateStr) + '</span>' : '') +
        '<div class="rp-item-sub">' + _esc(company) + '</div>' +
        (desc ? '<div style="color:#666;font-size:9px;margin-top:2px;">' + _esc(desc) + '</div>' : '') +
        '</div>';
    _showHideTitle('liveExpContainer');
}

// ═══════════════════════════════════════════════════════════════════════
// EDUCATION – live preview of the "Add New" form
// ═══════════════════════════════════════════════════════════════════════

function initEducationPreview() {
    var fields = ['eduInstitution', 'eduDegree', 'eduField', 'eduStart', 'eduEnd', 'eduGPA'];
    fields.forEach(function (fid) {
        var el = _el(fid);
        if (el) el.addEventListener('input', _updateEduPreview);
    });
    _updateEduPreview();
}

function _updateEduPreview() {
    var institution = _val('eduInstitution');
    var degree = _val('eduDegree');
    var field = _val('eduField');
    var startDate = _val('eduStart');
    var endDate = _val('eduEnd');
    var gpa = _val('eduGPA');

    var hasData = institution || degree;

    var container = _ensureSection('liveEduContainer', 'Education (typing...)');
    if (!container) return;

    if (!hasData) {
        container.innerHTML = '';
        _showHideTitle('liveEduContainer');
        return;
    }

    var startStr = startDate ? _formatDate(startDate) : '';
    var endStr = endDate ? _formatDate(endDate) : '';
    var dateStr = [startStr, endStr].filter(Boolean).join(' - ');

    container.innerHTML =
        '<div class="rp-item" style="opacity:0.7;border-left:2px solid #7c3aed;padding-left:6px;">' +
        '<span class="rp-item-title">' + _esc(degree) + (field ? ' — ' + _esc(field) : '') + '</span>' +
        (dateStr ? '<span class="rp-item-date">' + _esc(dateStr) + '</span>' : '') +
        '<div class="rp-item-sub">' + _esc(institution) + '</div>' +
        (gpa ? '<div style="color:#666;font-size:9px;margin-top:2px;">GPA: ' + _esc(gpa) + '</div>' : '') +
        '</div>';
    _showHideTitle('liveEduContainer');
}

// ═══════════════════════════════════════════════════════════════════════
// SKILLS – live preview of the "Add New" form
// ═══════════════════════════════════════════════════════════════════════

function initSkillsPreview() {
    var fields = ['skillName', 'skillLevel'];
    fields.forEach(function (fid) {
        var el = _el(fid);
        if (el) el.addEventListener('input', _updateSkillPreview);
        if (el && el.tagName === 'SELECT') el.addEventListener('change', _updateSkillPreview);
    });
    _updateSkillPreview();
}

function _updateSkillPreview() {
    var name = _val('skillName');
    var level = _val('skillLevel');

    var container = _ensureSection('liveSkillContainer', 'Skills (typing...)');
    if (!container) return;

    if (!name) {
        container.innerHTML = '';
        _showHideTitle('liveSkillContainer');
        return;
    }

    container.innerHTML =
        '<div style="display:inline-block;opacity:0.7;border:1px dashed #7c3aed;border-radius:6px;padding:2px 8px;margin:2px;font-size:9px;">' +
        _esc(name) + (level ? ' (' + _esc(level) + ')' : '') +
        '</div>';
    _showHideTitle('liveSkillContainer');
}

// ═══════════════════════════════════════════════════════════════════════
// PROJECTS – live preview of the "Add New" form
// ═══════════════════════════════════════════════════════════════════════

function initProjectsPreview() {
    var fields = ['projTitle', 'projTech', 'projUrl', 'projStart', 'projEnd', 'projDesc'];
    fields.forEach(function (fid) {
        var el = _el(fid);
        if (el) el.addEventListener('input', _updateProjPreview);
    });
    _updateProjPreview();
}

function _updateProjPreview() {
    var title = _val('projTitle');
    var tech = _val('projTech');
    var url = _val('projUrl');
    var desc = _val('projDesc');

    var hasData = title || tech || desc;

    var container = _ensureSection('liveProjContainer', 'Projects (typing...)');
    if (!container) return;

    if (!hasData) {
        container.innerHTML = '';
        _showHideTitle('liveProjContainer');
        return;
    }

    container.innerHTML =
        '<div class="rp-item" style="opacity:0.7;border-left:2px solid #7c3aed;padding-left:6px;">' +
        '<span class="rp-item-title">' + _esc(title) + '</span>' +
        (url ? '<span style="float:right;font-size:9px;color:#7c3aed;">link</span>' : '') +
        (tech ? '<div class="rp-item-sub">' + _esc(tech) + '</div>' : '') +
        (desc ? '<div style="color:#666;font-size:9px;margin-top:2px;">' + _esc(desc) + '</div>' : '') +
        '</div>';
    _showHideTitle('liveProjContainer');
}

// ═══════════════════════════════════════════════════════════════════════
// CERTIFICATIONS – live preview of the "Add New" form
// ═══════════════════════════════════════════════════════════════════════

function initCertificationsPreview() {
    var fields = ['certName', 'certOrg', 'certDate', 'certLink'];
    fields.forEach(function (fid) {
        var el = _el(fid);
        if (el) el.addEventListener('input', _updateCertPreview);
    });
    _updateCertPreview();
}

function _updateCertPreview() {
    var name = _val('certName');
    var org = _val('certOrg');
    var date = _val('certDate');

    var hasData = name || org;

    var container = _ensureSection('liveCertContainer', 'Certifications (typing...)');
    if (!container) return;

    if (!hasData) {
        container.innerHTML = '';
        _showHideTitle('liveCertContainer');
        return;
    }

    var sub = [org, date].filter(Boolean).join(' · ');

    container.innerHTML =
        '<div class="rp-item" style="opacity:0.7;border-left:2px solid #7c3aed;padding-left:6px;">' +
        '<span class="rp-item-title">' + _esc(name) + '</span>' +
        (sub ? '<div class="rp-item-sub">' + _esc(sub) + '</div>' : '') +
        '</div>';
    _showHideTitle('liveCertContainer');
}

// ═══════════════════════════════════════════════════════════════════════
// UTILITIES
// ═══════════════════════════════════════════════════════════════════════

function _formatDate(dateStr) {
    if (!dateStr) return '';
    var d = new Date(dateStr);
    if (isNaN(d)) return dateStr;
    return (d.getMonth() + 1) + '/' + d.getDate() + '/' + (d.getFullYear() % 100);
}

function _esc(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}