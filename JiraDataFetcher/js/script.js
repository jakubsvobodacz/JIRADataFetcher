document.addEventListener("DOMContentLoaded", function () {
    // Load epic data
    fetch('DataFetch/EpicData/epics.json')
        .then(response => response.json())
        .then(data => {
            // Create Bootstrap cards
            data.forEach(item => {
                const cardHtml = `
                   <div class="col d-flex align-items-stretch">
                        <div class="card shadow-sm flex-fill">
                            <div class="card-body">
                                <h5 class="card-title">${item.summary}</h5>
                                <p className="card-text">Updated: ${item.updated}</p>
                                <p className="card-text">Created: ${item.created}</p>
                                <p className="card-text">Due Date: ${item.dueDate}</p>
                            </div>
                        </div>
                    </div>
                `;
                document.getElementById('cardContainer').innerHTML += cardHtml;
            });
        })
        .catch(error => console.error('Error loading JSON:', error));
    
    // Load sprint data
    fetch('DataFetch/facts_sprint.json')
        .then(response => response.json())
        .then(data => {
                const cardsHtml = `
                   <div class="col d-flex align-items-stretch">
                        <div class="card shadow-sm flex-fill">
                            <div class="card-body">
                                <h5 class="card-title">OKR Contribution</h5>
                                <p className="card-text fs-1">${data.epicRatio} %</p>
                            </div>
                        </div>
                    </div>
                    <div class="col d-flex align-items-stretch">
                        <div class="card shadow-sm flex-fill">
                            <div class="card-body">
                                <h5 class="card-title">Bug Contribution</h5>
                                <p className="card-text">${data.bugRatio} %</p>
                            </div>
                        </div>
                    </div>
                    <div class="col d-flex align-items-stretch">
                        <div class="card shadow-sm flex-fill">
                            <div class="card-body">
                                <h5 class="card-title">Maintenance Contribution</h5>
                                <p className="card-text">${data.maintenanceRatio} %</p>
                            </div>
                        </div>
                    </div>
                    <div class="col d-flex align-items-stretch">
                        <div class="card shadow-sm flex-fill">
                            <div class="card-body">
                                <h5 class="card-title">Other Issues Contribution</h5>
                                <p className="card-text">${data.otherIssuesRatio} %</p>
                            </div>
                        </div>
                    </div>
                `;
                document.getElementById('cardContainerSprint').innerHTML += cardsHtml;
            
        })
        .catch(error => console.error('Error loading JSON:', error));
});




