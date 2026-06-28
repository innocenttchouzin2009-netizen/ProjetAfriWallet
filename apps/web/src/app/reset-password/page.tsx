import ResetPasswordForm from '@/features/auth/components/ResetPasswordForm';

export default function ResetPasswordPage() {
  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <div className="mx-auto max-w-2xl">
        <ResetPasswordForm />
      </div>
    </main>
  );
}
