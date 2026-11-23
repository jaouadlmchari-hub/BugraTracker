import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'https://localhost:7016';
const EMAIL = __ENV.EMAIL || 'admin@bugtracker.com';
const PASSWORD = __ENV.PASSWORD || 'Admin123!';
const PROJECT_ID = __ENV.PROJECT_ID || ''; 
const ISSUE_ID = __ENV.ISSUE_ID || '';     

const tProjects = new Trend('t_projects', true);
const tProjectIssues = new Trend('t_project_issues', true);
const tIssueDetail = new Trend('t_issue_detail', true);
const tMembers = new Trend('t_members', true);
const tSprints = new Trend('t_sprints', true);
const tComments = new Trend('t_comments', true);
const tUsers = new Trend('t_users', true);
const tLogin = new Trend('t_login', true);

export const options = {
    insecureSkipTLSVerify: true,
    scenarios: {
        reads: {
            executor: 'constant-vus',
            vus: Number(__ENV.VUS || 1),
            duration: __ENV.DURATION || '30s',
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.05'],
        http_req_duration: ['p(95)<3000'],
    },
};

function login() {
    const res = http.post(
        `${BASE_URL}/api/Auth/login`,
        JSON.stringify({ email: EMAIL, password: PASSWORD }),
        {
            headers: { 'Content-Type': 'application/json' },
            tags: { name: 'login' },
        }
    );

    tLogin.add(res.timings.duration);

    check(res, {
        'login 200': (r) => r.status === 200,
    });

    if (res.status !== 200) return null;

    const body = res.json();
    return body.accessToken || body.AccessToken || null;
}

function authHeaders(token) {
    return {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
    };
}

export default function () {
    const token = login();
    if (!token) {
        sleep(1);
        return;
    }

    const headers = authHeaders(token);

    // 4 — GET /api/Projects
    group('GET /api/Projects', () => {
        const res = http.get(`${BASE_URL}/api/Projects`, {
            headers,
            tags: { name: 'projects_list' },
        });
        tProjects.add(res.timings.duration);
        check(res, { 'projects 200': (r) => r.status === 200 });
    });

    // 1 & 2 — GET /api/projects/{projectId}/issues
    if (PROJECT_ID) {
        group('GET /api/projects/{id}/issues', () => {
            const res = http.get(`${BASE_URL}/api/projects/${PROJECT_ID}/issues`, {
                headers,
                tags: { name: 'project_issues' },
            });
            tProjectIssues.add(res.timings.duration);
            check(res, { 'project issues 200': (r) => r.status === 200 });
        });

        // 5 — members
        group('GET /api/projects/{id}/members', () => {
            const res = http.get(`${BASE_URL}/api/projects/${PROJECT_ID}/members`, {
                headers,
                tags: { name: 'project_members' },
            });
            tMembers.add(res.timings.duration);
            check(res, { 'members 200': (r) => r.status === 200 });
        });

        // 6 — sprints
        group('GET /api/projects/{id}/sprints', () => {
            const res = http.get(`${BASE_URL}/api/projects/${PROJECT_ID}/sprints`, {
                headers,
                tags: { name: 'project_sprints' },
            });
            tSprints.add(res.timings.duration);
            check(res, { 'sprints 200': (r) => r.status === 200 });
        });
    }

    // 3 — issue detail 
    if (ISSUE_ID) {
        group('GET /api/issues/{id}', () => {
            const res = http.get(`${BASE_URL}/api/issues/${ISSUE_ID}`, {
                headers,
                tags: { name: 'issue_detail' },
            });
            tIssueDetail.add(res.timings.duration);
            check(res, { 'issue detail 200': (r) => r.status === 200 });
        });

        // 7 — comments 
        group('GET /api/issues/{id}/comments', () => {
            const res = http.get(`${BASE_URL}/api/issues/${ISSUE_ID}/comments`, {
                headers,
                tags: { name: 'issue_comments' },
            });
            tComments.add(res.timings.duration);
            check(res, { 'comments 200': (r) => r.status === 200 });
        });
    }

    // 8 — users
    group('GET /api/Users', () => {
        const res = http.get(`${BASE_URL}/api/Users`, {
            headers,
            tags: { name: 'users_list' },
        });
        tUsers.add(res.timings.duration);
        check(res, { 'users 200': (r) => r.status === 200 });
    });

    sleep(1);
}