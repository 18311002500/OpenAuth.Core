using Infrastructure.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OpenAuth.App.Interface;
using OpenAuth.App.Response;
using System;

namespace OpenAuth.App.SSO
{
    /// <summary>
    /// ʹ�ñ��ص�¼�����ע��IAuthʱ��ֻ��ҪOpenAuth.Mvcһ����Ŀ���ɣ�����webapi��֧��
    /// </summary>
    public class LocalAuth : IAuth
    {
        private IOptions<AppSetting> _appConfiguration;
        private IHttpContextAccessor _httpContextAccessor;

        private AuthorizeApp _app;
        private LoginParse _loginParse;
        private ICacheContext _cacheContext;

        public LocalAuth(IOptions<AppSetting> appConfiguration
            , IHttpContextAccessor httpContextAccessor
            , AuthorizeApp app
            , LoginParse loginParse
            , ICacheContext cacheContext)
        {
            _appConfiguration = appConfiguration;
            _httpContextAccessor = httpContextAccessor;
            _app = app;
            _loginParse = loginParse;
            _cacheContext = cacheContext;
        }

        private string GetToken()
        {
            string token = _httpContextAccessor.HttpContext.Request.Query["Token"];
            if (!String.IsNullOrEmpty(token)) return token;

            var cookie = _httpContextAccessor.HttpContext.Request.Cookies["Token"];
            return cookie ?? String.Empty;
        }

        public bool CheckLogin(string token = "", string otherInfo = "")
        {
            if (string.IsNullOrEmpty(token))
            {
                token = GetToken();
            }

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                var result = _cacheContext.Get<UserAuthSession>(token) != null;
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// ��ȡ��ǰ��¼���û���Ϣ
        /// <para>ͨ��URL�е�Token������Cookie�е�Token</para>
        /// </summary>
        /// <param name="otherInfo">The otherInfo.</param>
        /// <returns>LoginUserVM.</returns>
        public UserWithAccessedCtrls GetCurrentUser(string otherInfo = "")
        {
            UserWithAccessedCtrls userctrls = null;
            var user = _cacheContext.Get<UserAuthSession>(GetToken());
            if (user != null)
            {
                string ctrlskey = GetToken() + "_CTRLS";
                userctrls = _cacheContext.Get<UserWithAccessedCtrls>(ctrlskey);
                if (userctrls == null)
                {
                    userctrls = _app.GetAccessedControls(user.Account);
                    _cacheContext.Set(ctrlskey, userctrls, DateTime.Now.AddMinutes(10));
                }
            }
            return userctrls;
        }

        /// <summary>
        /// ��ȡ��ǰ��¼���û���
        /// <para>ͨ��URL�е�Token������Cookie�е�Token</para>
        /// </summary>
        /// <param name="otherInfo">The otherInfo.</param>
        /// <returns>System.String.</returns>
        public string GetUserName(string otherInfo = "")
        {
                var user = _cacheContext.Get<UserAuthSession>(GetToken());
                if (user != null)
                {
                    return user.Account;
                }

                return "";
        }

        /// <summary>
        /// ��¼�ӿ�
        /// </summary>
        /// <param name="appKey">Ӧ�ó���key.</param>
        /// <param name="username">�û���</param>
        /// <param name="pwd">����</param>
        /// <returns>System.String.</returns>
        public LoginResult Login(string appKey, string username, string pwd)
        {
                return _loginParse.Do(new PassportLoginRequest
                {
                    AppKey = appKey,
                    Account = username,
                    Password = pwd
                });
        }

        /// <summary>
        /// ע��
        /// </summary>
        public bool Logout()
        {
            var token = GetToken();
            if (String.IsNullOrEmpty(token)) return true;

            try
            {
                _cacheContext.Remove(token);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}