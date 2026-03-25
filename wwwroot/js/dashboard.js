let regionChart, monthlyChart, categoryChart, forecastChart;

function loadDashboard() {

    $("#loaderOverlay").show();

    const region = document.getElementById("regionFilter").value;
    const category = document.getElementById("categoryFilter").value;

    fetch(`/filtered-data?region=${region}&category=${category}`)
        .then(res => res.json())
        .then(data => {

            // KPI
            document.getElementById('totalRevenue').innerText = data.totalRevenue;
            document.getElementById('totalOrders').innerText = data.totalOrders;
            document.getElementById('topRegion').innerText = data.topRegion?.region;
            document.getElementById('topCategory').innerText = data.topCategory?.category;

            // Destroy old charts
            if (regionChart) regionChart.destroy();
            if (monthlyChart) monthlyChart.destroy();
            if (categoryChart) categoryChart.destroy();
            if (forecastChart) forecastChart.destroy();

            // Region Chart
            regionChart = new Chart(document.getElementById('regionChart'), {
                type: 'bar',
                data: {
                    labels: data.regionSales.map(x => x.region),
                    datasets: [{
                        label: 'Sales by Region',
                        data: data.regionSales.map(x => x.total),
                        backgroundColor: ['#3498db', '#e74c3c', '#f1c40f', '#2ecc71']
                    }]
                }
            });

            // Monthly Chart
            monthlyChart = new Chart(document.getElementById('monthlyChart'), {
                type: 'line',
                data: {
                    labels: data.monthlySales.map(x => "Month " + x.month),
                    datasets: [{
                        label: 'Monthly Sales',
                        data: data.monthlySales.map(x => x.total),
                        borderColor: '#3498db',
                        backgroundColor: 'rgba(52,152,219,0.2)',
                        fill: true
                    }]
                }
            });

            // Category Chart
            categoryChart = new Chart(document.getElementById('categoryChart'), {
                type: 'pie',
                data: {
                    labels: data.categorySales.map(x => x.category),
                    datasets: [{
                        data: data.categorySales.map(x => x.total),
                        backgroundColor: ['#3498db', '#e74c3c', '#f39c12', '#2ecc71']
                    }]
                }
            });

            // Forecast Chart
            forecastChart = new Chart(document.getElementById('forecastChart'), {
                type: 'line',
                data: {
                    labels: data.forecast.map(x => x.month),
                    datasets: [{
                        label: 'Sales Forecast',
                        data: data.forecast.map(x => x.total),
                        borderColor: '#8e44ad',
                        backgroundColor: 'rgba(142,68,173,0.2)',
                        fill: true
                    }]
                }
            });

            // Heatmap with filters
            drawHeatmap(region, category);

            $("#loaderOverlay").hide();
        });
}

// Reset Filters
function resetFilter() {
    document.getElementById("regionFilter").value = "";
    document.getElementById("categoryFilter").value = "";
    loadDashboard();
}

// Heatmap Function
function drawHeatmap(region, category) {

    fetch(`/api/sales/heatmap?region=${region}&category=${category}`)
        .then(res => res.json())
        .then(heatmapData => {

            const canvas = document.getElementById("heatmapChart");
            if (!canvas) return;

            const ctx = canvas.getContext("2d");

            const labels = ["Sales", "Price", "Quantity", "Month"];
            const size = 80;   // Bigger boxes
            const startX = 120;
            const startY = 40;

            ctx.clearRect(0, 0, canvas.width, canvas.height);

            for (let i = 0; i < heatmapData.length; i++) {
                for (let j = 0; j < heatmapData[i].length; j++) {

                    let value = heatmapData[i][j];

                    // Softer blue gradient
                    let intensity = Math.abs(value);
                    let color = `rgba(52, 152, 219, ${intensity})`;

                    ctx.fillStyle = color;
                    ctx.fillRect(startX + j * size, startY + i * size, size, size);

                    ctx.fillStyle = "#2c3e50";
                    ctx.font = "14px Arial";
                    ctx.fillText(value.toFixed(2), startX + j * size + 20, startY + i * size + 45);
                }

                // Y-axis labels
                ctx.fillStyle = "#2c3e50";
                ctx.font = "14px Arial";
                ctx.fillText(labels[i], 40, startY + i * size + 45);
            }

            // X-axis labels
            for (let i = 0; i < labels.length; i++) {
                ctx.fillText(labels[i], startX + i * size + 20, startY + 4 * size + 20);
            }
        });
}