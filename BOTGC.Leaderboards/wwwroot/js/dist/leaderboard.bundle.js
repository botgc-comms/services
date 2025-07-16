
/**
 * Updates the leaderboard table rows in-place, matching player position to row.
 * @param {number} competitionId - The competition ID to fetch data for.
 * @param {number} [rowCount=25] - The number of rows to display.
 * @param {string} [endpoint='/Leaderboard/GetLeaderboardData'] - The endpoint to fetch player data.
 */
function updateLeaderboardRows(competitionId, rowCount = 25, endpoint = '/Leaderboard/GetLeaderboardData') {
    fetch(`${endpoint}?competitionId=${competitionId}`)
        .then(response => {
            if (!response.ok) throw new Error('Failed to fetch leaderboard data');
            return response.json();
        })
        .then(players => {
            // Map players by position for quick lookup
            const playerMap = {};
            players.forEach(player => {
                playerMap[player.position] = player;
            });

            console.log(`Updating Leaderboard..... ${new Date().toLocaleString()}`);

            // Update each row by position
            for (let i = 1; i <= rowCount; i++) {
                const row = document.getElementById(`row-${i}`);
                if (!row) continue;
                const cells = row.querySelectorAll('td');
                const player = playerMap[i];
                cells[0].textContent = i;
                if (player) {
                    cells[1].textContent = `${player.names} (${player.playingHandicap})`;
                    cells[2].textContent = player.score;
                } else {
                    cells[1].textContent = '';
                    cells[2].textContent = '';
                }
            }
        })
        .catch(error => {
            // Optionally log error
            // console.error(error);
        });
}

window.updateLeaderboardRows = updateLeaderboardRows;

