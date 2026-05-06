export default function FormField({ label, error, children }) {
  return (
    <div className="form-group">
      <label>{label}</label>
      {children}
      {(error || []).map((item) => (
        <div className="invalid-feedback d-block" key={item}>{item}</div>
      ))}
    </div>
  );
}
