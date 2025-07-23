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

function formatScore(score) {
    if (score === null || score === undefined || score === "") return "";
    if (score.trim() === "0" || score.trim().toUpperCase() === "LEVEL") return "LEVEL";
    return score;
}

function shouldShowR1(thru) {
    return thru >= 18;
}

function shouldShowR2(thru) {
    return thru === 36;
}

function formatThru(thru) {
    return thru === 36 ? "F" : (thru !== undefined && thru !== null ? thru : "");
}

function setParCell(cell, par) {
    let value = formatScore(par);
    cell.textContent = value;
    cell.classList.remove("score-below-par");
    let parVal = 0;
    if (typeof par === "string") {
        let p = par.trim().toUpperCase();
        if (p === "LEVEL") parVal = 0;
        else if (p.startsWith("+")) parVal = parseInt(p.substring(1), 10);
        else if (p.startsWith("-")) parVal = parseInt(p, 10);
        else parVal = parseInt(p, 10);
    }
    if (parVal < 0) cell.classList.add("score-below-par");
}

function updateClubChampionshipLeaderboardRows(
    competitionId, rowCount = 25, endpoint = '/ClubChampionshipLeaderBoard/GetLeaderboardData'
) {
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
                    cells[1].textContent = player.name ?? "";
                    cells[2].textContent = formatThru(player.thru);
                    cells[3].textContent = shouldShowR1(player.thru) ? formatScore(player.r1) : "-";
                    cells[4].textContent = shouldShowR2(player.thru) ? formatScore(player.r2) : "-";
                    setParCell(cells[5], player.par);
                } else {
                    cells[1].textContent = '';
                    cells[2].textContent = '';
                    cells[3].textContent = '';
                    cells[4].textContent = '';
                    cells[5].textContent = '';
                    cells[5].classList.remove("score-below-par");
                }
            }
        })
        .catch(error => {
            // Optionally log error
            // console.error(error);
        });
}

// Do not forget to export the correct function name for your polling code:
window.updateClubChampionshipLeaderboardRows = updateClubChampionshipLeaderboardRows;
