import { useState, useEffect } from 'react'
import './App.css'

function App() {
  const [downloads, setDownloads] = useState([])
  const [isUploading, setIsUploading] = useState(false)
  const [file, setFile] = useState(null)

  
  const API_BASE = "http://localhost:5259/api/torrents"

  useEffect(() => {
    fetchDownloads()
    const interval = setInterval(fetchDownloads, 1000)
    return () => clearInterval(interval)
  }, [])

  const fetchDownloads = async () => {
    try {
      const response = await fetch(API_BASE)
      if (response.ok) {
        const data = await response.json()
        setDownloads(data)
      }
    } catch (error) {
      console.error("Error fetching downloads:", error)
    }
  }

  const handleFileChange = (e) => {
    setFile(e.target.files[0])
  }

  const handleUpload = async () => {
    if (!file) return

    setIsUploading(true)
    const formData = new FormData()
    formData.append('file', file)

    try {
      const response = await fetch(API_BASE, {
        method: 'POST',
        body: formData
      })
      
      if (response.ok) {
        setFile(null)
     
        document.getElementById('fileInput').value = ""
        fetchDownloads() 
      } else {
        alert("Error al subir el torrent")
      }
    } catch {
      alert("Error de conexión")
    } finally {
      setIsUploading(false)
    }
  }

 
  const formatBytes = (bytes, decimals = 2) => {
    if (!+bytes) return '0 Bytes'
    const k = 1024
    const dm = decimals < 0 ? 0 : decimals
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`
  }


  const getStateName = (stateEnum) => {
    const states = ["Queued", "Downloading", "Completed", "Failed", "Cancelled"]
    return states[stateEnum] || "Unknown"
  }

  return (
    <div className="container">
      <h1>TorrentClient <span style={{fontSize: '0.5em', opacity: 0.5}}>v1.0</span></h1>

      {/* Upload Area */}
      <div className="upload-card">
        <input 
          id="fileInput"
          type="file" 
          accept=".torrent" 
          onChange={handleFileChange} 
        />
        <button 
          onClick={handleUpload} 
          disabled={!file || isUploading}
        >
          {isUploading ? 'Subiendo...' : 'Descargar Torrent'}
        </button>
      </div>


      <div className="download-list">
        {downloads.length === 0 ? (
          <div className="empty-state">
            No hay descargas activas. ¡Subi un .torrent para empezar! 
          </div>
        ) : (
          downloads.map((d) => {
            const stateName = getStateName(d.state)
            const progress = d.percentage || 0
            
            return (
              <div key={d.id} className="download-item">
                <div className="download-header">
                  <div className="filename">{d.torrentName || 'Cargando metadatos...'}</div>
                  <span className={`badge ${stateName.toLowerCase()}`}>
                    {stateName}
                  </span>
                </div>

                <div className="progress-track">
                  <div 
                    className="progress-fill" 
                    style={{ width: `${progress}%` }}
                  ></div>
                </div>

                <div className="meta-info">
                  <span> {d.completedPieces} / {d.totalPieces} piezas</span>
                  <span>{progress.toFixed(1)}%</span>
                  <span>{formatBytes(d.totalSize)}</span>
                </div>
                
                {d.error && <div style={{color: '#ef4444', marginTop: '10px', fontSize: '0.9em'}}>❌ {d.error}</div>}
              </div>
            )
          })
        )}
      </div>
    </div>
  )
}

export default App