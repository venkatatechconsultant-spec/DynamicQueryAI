import React from 'react';
import ReactDOM from 'react-dom/client';
import { Send, Database, BarChart3 } from 'lucide-react';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid
} from 'recharts';
import './styles.css';

type ChartDefinition = {
  type: string;
  xAxis: string;
  yAxis: string;
  title: string;
};

type ChatResponse = {
  conversationId: string;
  intent: string;
  sql: string;
  explanation: string;
  rows: Record<string, unknown>[];
  chart?: ChartDefinition;
  warnings: string[];
};

type Message = {
  role: 'user' | 'assistant';
  text: string;
  response?: ChatResponse;
};

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7194';

function App() {
  const [messages, setMessages] = React.useState<Message[]>([
    {
      role: 'assistant',
      text: 'Ask for daily, weekly, monthly, or quarterly sales, usage, inspection, media, or voucher trends.'
    }
  ]);
  const [input, setInput] = React.useState('Show monthly sales trend for the last 12 months');
  const [conversationId, setConversationId] = React.useState<string | undefined>();
  const [isLoading, setIsLoading] = React.useState(false);

  /// Sends the user's natural-language analytics request to the .NET API and appends the chart-ready response.
  async function submitQuestion(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = input.trim();
    if (!trimmed || isLoading) return;

    setMessages((current) => [...current, { role: 'user', text: trimmed }]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await fetch(`${apiBaseUrl}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: trimmed, conversationId })
      });

      if (!response.ok) {
        throw new Error(`API returned ${response.status}`);
      }

      const data = (await response.json()) as ChatResponse;
      setConversationId(data.conversationId);
      setMessages((current) => [
        ...current,
        {
          role: 'assistant',
          text: data.explanation,
          response: data
        }
      ]);
    } catch (error) {
      setMessages((current) => [
        ...current,
        {
          role: 'assistant',
          text: error instanceof Error ? error.message : 'Unable to reach analytics API.'
        }
      ]);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <Database size={22} />
          <span>DB Query AI Engine</span>
        </div>
        <button onClick={() => setInput('Show daily usage trend for the last 30 days')}>Daily usage</button>
        <button onClick={() => setInput('Show weekly sales analysis for the last 12 weeks')}>Weekly sales</button>
        <button onClick={() => setInput('Show quarterly voucher trend for the last 8 quarters')}>Quarterly vouchers</button>
      </aside>

      <section className="chat-panel">
        <header className="topbar">
          <div>
            <h1>Fabric DW Analytics Chat</h1>
            <p>Template-guarded SQL, warehouse aggregates, and AI explanations.</p>
          </div>
          <BarChart3 size={28} />
        </header>

        <div className="messages">
          {messages.map((message, index) => (
            <article key={index} className={`message ${message.role}`}>
              <p>{message.text}</p>
              {message.response && <InsightResult response={message.response} />}
            </article>
          ))}
          {isLoading && <article className="message assistant">Analyzing warehouse trend...</article>}
        </div>

        <form className="composer" onSubmit={submitQuestion}>
          <input
            value={input}
            onChange={(event) => setInput(event.target.value)}
            placeholder="Ask: monthly sales trend by region for last 12 months"
          />
          <button type="submit" disabled={isLoading} aria-label="Send question">
            <Send size={18} />
          </button>
        </form>
      </section>
    </main>
  );
}

/// Renders SQL, chart, and tabular preview returned by the analytics API.
function InsightResult({ response }: { response: ChatResponse }) {
  const chartRows = response.rows.map((row) => ({
    ...row,
    PeriodStart: String(row.PeriodStart ?? ''),
    MetricValue: Number(row.MetricValue ?? 0)
  }));

  return (
    <div className="result">
      {response.chart && (
        <div className="chart-wrap">
          <h2>{response.chart.title}</h2>
          <ResponsiveContainer width="100%" height={280}>
            {response.chart.type === 'bar' ? (
              <BarChart data={chartRows}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey={response.chart.xAxis} />
                <YAxis />
                <Tooltip />
                <Bar dataKey={response.chart.yAxis} fill="#2563eb" />
              </BarChart>
            ) : (
              <LineChart data={chartRows}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey={response.chart.xAxis} />
                <YAxis />
                <Tooltip />
                <Line type="monotone" dataKey={response.chart.yAxis} stroke="#0f766e" strokeWidth={3} dot={false} />
              </LineChart>
            )}
          </ResponsiveContainer>
        </div>
      )}

      <details>
        <summary>Generated SQL</summary>
        <pre>{response.sql}</pre>
      </details>

      <div className="table-scroll">
        <table>
          <thead>
            <tr>
              {Object.keys(response.rows[0] ?? {}).map((key) => (
                <th key={key}>{key}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {response.rows.slice(0, 20).map((row, index) => (
              <tr key={index}>
                {Object.values(row).map((value, cellIndex) => (
                  <td key={cellIndex}>{String(value ?? '')}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
