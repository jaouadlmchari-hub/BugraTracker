import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = 'https://localhost:7016';
const TOKEN = __ENV.JWT_TOKEN;
// Remplace par un vrai ID de projet dont ton utilisateur est membre
const PROJECT_ID = 'F453EE67-FDDA-4A64-88A3-04B1E84B5546';

export const options = {
    stages: [
        { duration: '10s', target: 5 },
        { duration: '20s', target: 10 },
        { duration: '10s', target: 0 },
    ],
};

export default function () {
    const response = http.get(
        `${BASE_URL}/api/projects/${PROJECT_ID}/issues`,
        {
            headers: {
                Authorization: `Bearer ${TOKEN}`,
            },
            tags: { endpoint: 'GET /api/projects/{projectId}/issues' },
            timeout: '30s',
            insecureSkipTLSVerify: true,
        }
    );

    // Si tu obtiens encore une erreur, cela affichera le code et le message exact dans ta console
    if (response.status !== 200) {
        console.log(`Erreur HTTP ${response.status}: ${response.body}`);
    }

    check(response, {
        'status is 200': (r) => r.status === 200,
    });

    sleep(1);
}